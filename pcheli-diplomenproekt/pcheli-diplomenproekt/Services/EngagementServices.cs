using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

// ============================ Контакти ============================
public interface IContactService
{
    Task SubmitAsync(ContactDto dto);
    Task<List<ContactMessageDto>> GetAllAsync();
    Task<ContactMessageDto> SetHandledAsync(int id, bool handled);
    Task DeleteAsync(int id);
}

/// <summary>Съобщения от формата „Контакти“: записват се в базата и се препращат по имейл на магазина (и на пчеларя, ако е избран).</summary>
public class ContactService : IContactService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService? _notify;
    private readonly string? _shopEmail;

    public ContactService(ApplicationDbContext db, IConfiguration config, INotificationService? notify = null)
    {
        _db = db; _notify = notify;
        _shopEmail = config["Shop:ContactEmail"] ?? config["Admin:Email"];
    }

    public async Task SubmitAsync(ContactDto dto)
    {
        string? beekeeperEmail = null;
        if (dto.BeekeeperId.HasValue)
        {
            var bk = await _db.Beekeepers.AsNoTracking().Where(b => b.Id == dto.BeekeeperId.Value && b.IsActive)
                         .Select(b => new { b.Email }).FirstOrDefaultAsync()
                     ?? throw new BusinessException("Пчеларят не е намерен.", StatusCodes.Status404NotFound);
            beekeeperEmail = bk.Email;
        }

        var m = new ContactMessage
        {
            Name = dto.Name.Trim(), Email = dto.Email.Trim(), Phone = dto.Phone?.Trim(), Subject = dto.Subject.Trim(),
            Message = dto.Message.Trim(), BeekeeperId = dto.BeekeeperId, CreatedAt = DateTime.UtcNow
        };
        _db.ContactMessages.Add(m);
        await _db.SaveChangesAsync();
        _notify?.ContactReceived(m, _shopEmail, beekeeperEmail);
    }

    private static ContactMessageDto ToDto(ContactMessage m) => new(m.Id, m.Name, m.Email, m.Phone, m.Subject, m.Message,
        m.BeekeeperId, m.Beekeeper?.ApiaryName, m.CreatedAt, m.IsHandled);

    public async Task<List<ContactMessageDto>> GetAllAsync() =>
        (await _db.ContactMessages.AsNoTracking().Include(m => m.Beekeeper)
            .OrderBy(m => m.IsHandled).ThenByDescending(m => m.CreatedAt).Take(500).ToListAsync()).Select(ToDto).ToList();

    public async Task<ContactMessageDto> SetHandledAsync(int id, bool handled)
    {
        var m = await _db.ContactMessages.Include(x => x.Beekeeper).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException("Съобщението не е намерено.", StatusCodes.Status404NotFound);
        m.IsHandled = handled;
        await _db.SaveChangesAsync();
        return ToDto(m);
    }

    public async Task DeleteAsync(int id) => await _db.ContactMessages.Where(m => m.Id == id).ExecuteDeleteAsync();
}

// ============================ Бюлетин ============================
public interface INewsletterService
{
    Task SubscribeAsync(string email);
    Task<bool> UnsubscribeAsync(string token);
    Task<List<NewsletterSubscriberDto>> GetAllAsync();
    Task<int> SendAsync(NewsletterSendDto dto, string baseUrl);
}

public class NewsletterService : INewsletterService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService? _notify;
    public NewsletterService(ApplicationDbContext db, INotificationService? notify = null) { _db = db; _notify = notify; }

    public async Task SubscribeAsync(string email)
    {
        email = email.Trim();
        var existing = await _db.NewsletterSubscribers.FirstOrDefaultAsync(s => s.Email == email.ToLower());
        if (existing is null)
            _db.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = email.ToLowerInvariant() });
        else if (!existing.IsActive)
            existing.IsActive = true;   // повторно записване
        else return;                    // идемпотентно – без разкриване дали имейлът е вече записан
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException) { /* едновременен запис на същия имейл – резултатът е същият */ }
    }

    public async Task<bool> UnsubscribeAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var s = await _db.NewsletterSubscribers.FirstOrDefaultAsync(x => x.Token == token);
        if (s is null) return false;
        s.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<NewsletterSubscriberDto>> GetAllAsync() =>
        await _db.NewsletterSubscribers.AsNoTracking().OrderByDescending(s => s.CreatedAt)
            .Select(s => new NewsletterSubscriberDto(s.Id, s.Email, s.IsActive, s.CreatedAt)).ToListAsync();

    public async Task<int> SendAsync(NewsletterSendDto dto, string baseUrl)
    {
        var subs = await _db.NewsletterSubscribers.AsNoTracking().Where(s => s.IsActive).ToListAsync();
        if (subs.Count == 0) throw new BusinessException("Няма активни абонати.");
        var html = string.Concat(dto.Body.Replace("\r", "").Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => "<p>" + HtmlText.Encode(p).Replace("\n", "<br>") + "</p>"));
        foreach (var s in subs)
            _notify?.Newsletter(s.Email, dto.Subject.Trim(), html, $"{baseUrl.TrimEnd('/')}/api/newsletter/unsubscribe?token={s.Token}");
        return subs.Count;
    }
}

// ============================ „Уведоми ме“ за наличност ============================
public interface IStockAlertService
{
    Task SubscribeAsync(int productId, string email);
    Task NotifyAvailableAsync(int productId);
}

public class StockAlertService : IStockAlertService
{
    public const int MaxPerEmail = 20;
    private readonly ApplicationDbContext _db;
    private readonly INotificationService? _notify;
    private readonly string _baseUrl;

    public StockAlertService(ApplicationDbContext db, IConfiguration config, INotificationService? notify = null)
    {
        _db = db; _notify = notify;
        _baseUrl = (config["App:PublicUrl"] ?? string.Empty).TrimEnd('/');
    }

    public async Task SubscribeAsync(int productId, string email)
    {
        email = email.Trim().ToLowerInvariant();
        var p = await _db.Products.AsNoTracking().Include(x => x.Variants).FirstOrDefaultAsync(x => x.Id == productId)
                ?? throw new BusinessException("Продуктът не е намерен.", StatusCodes.Status404NotFound);
        if (p.InStock && p.Variants.Any(v => v.InStock && v.StockQuantity > 0))
            throw new BusinessException("Продуктът е наличен – можете да го поръчате веднага.");
        if (await _db.StockAlerts.AnyAsync(a => a.ProductId == productId && a.Email == email)) return;
        if (await _db.StockAlerts.CountAsync(a => a.Email == email) >= MaxPerEmail)
            throw new BusinessException("Достигнахте максималния брой известия за този имейл.");

        _db.StockAlerts.Add(new StockAlert { ProductId = productId, Email = email });
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException) { /* едновременно двойно записване – вече е в списъка */ }
    }

    public async Task NotifyAvailableAsync(int productId)
    {
        var alerts = await _db.StockAlerts.Where(a => a.ProductId == productId).Include(a => a.Product).ToListAsync();
        if (alerts.Count == 0) return;
        var url = $"{_baseUrl}/p/{productId}";
        foreach (var a in alerts) _notify?.StockAvailable(a.Email, a.Product?.Name ?? "Продуктът", url);
        _db.StockAlerts.RemoveRange(alerts);
        await _db.SaveChangesAsync();
    }
}

// ============================ Региони (администрация) ============================
public static class RegionAdmin
{
    public static async Task<RegionDto> CreateAsync(ApplicationDbContext db, RegionUpsertDto dto)
    {
        var name = dto.Name.Trim();
        if (await db.Regions.AnyAsync(r => r.Name == name))
            throw new BusinessException("Вече има регион с това име.", StatusCodes.Status409Conflict);
        var r = new Region { Name = name, Description = dto.Description.Trim() };
        db.Regions.Add(r);
        await db.SaveChangesAsync();
        return new RegionDto(r.Id, r.Name, r.Description, 0);
    }

    public static async Task<RegionDto> UpdateAsync(ApplicationDbContext db, int id, RegionUpsertDto dto)
    {
        var r = await db.Regions.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException("Регионът не е намерен.", StatusCodes.Status404NotFound);
        var name = dto.Name.Trim();
        if (await db.Regions.AnyAsync(x => x.Name == name && x.Id != id))
            throw new BusinessException("Вече има регион с това име.", StatusCodes.Status409Conflict);
        r.Name = name; r.Description = dto.Description.Trim();
        await db.SaveChangesAsync();
        return new RegionDto(r.Id, r.Name, r.Description, await db.Beekeepers.CountAsync(b => b.RegionId == id));
    }

    public static async Task DeleteAsync(ApplicationDbContext db, int id)
    {
        var r = await db.Regions.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException("Регионът не е намерен.", StatusCodes.Status404NotFound);
        if (await db.Beekeepers.AnyAsync(b => b.RegionId == id) || await db.BeekeeperApplications.AnyAsync(a => a.RegionId == id))
            throw new BusinessException("Към региона има пчелари или заявки – преместете ги първо в друг регион.", StatusCodes.Status409Conflict);
        db.Regions.Remove(r);
        await db.SaveChangesAsync();
    }
}

// ============================ Изтекли онлайн плащания ============================
/// <summary>
/// Поръчка с карта задържа наличностите си още при създаването. Ако не бъде платена в срок
/// (Payments:PendingHours, по подразбиране 24 – колкото е валидна сесията на Stripe), се отказва и наличностите се връщат.
/// </summary>
public class PendingPaymentCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PendingPaymentCleanupService> _logger;
    private readonly TimeSpan _maxAge;

    public PendingPaymentCleanupService(IServiceScopeFactory scopes, IConfiguration config, ILogger<PendingPaymentCleanupService> logger)
    {
        _scopes = scopes; _logger = logger;
        _maxAge = TimeSpan.FromHours(Math.Max(1, config.GetValue("Payments:PendingHours", 24)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var n = await orders.CancelStalePendingPaymentsAsync(_maxAge);
                if (n > 0) _logger.LogInformation("Отказани са {Count} поръчки без плащане; наличностите са върнати.", n);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Неуспешно почистване на неплатени поръчки.");
            }
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
