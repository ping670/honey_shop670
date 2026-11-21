using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

public interface ISubscriptionService
{
    Task<List<SubscriptionDto>> GetForUserAsync(string userId);
    Task<List<SubscriptionDto>> GetAllAsync();
    Task<SubscriptionDto> CreateAsync(string userId, SubscriptionCreateDto dto);
    Task<SubscriptionDto> SetStatusAsync(string userId, int id, string action);
    /// <summary>Създава поръчки за всички абонаменти, чиято дата на доставка е настъпила. Връща броя създадени поръчки.</summary>
    Task<int> ProcessDueAsync(DateTime nowUtc);
}

/// <summary>
/// Абонамент за регулярна доставка на мед (на 1, 2 или 3 месеца) с отстъпка. Плаща се с наложен платеж.
/// Поръчките се създават автоматично от фоновата услуга и минават през същата логика като обикновените (наличности, цени, известия).
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    public const int DiscountPercent = 5;
    public const int MaxActivePerUser = 5;

    private readonly ApplicationDbContext _db;
    private readonly IOrderService _orders;
    private readonly IShippingService _shipping;
    private readonly ILogger<SubscriptionService>? _logger;

    public SubscriptionService(ApplicationDbContext db, IOrderService orders, IShippingService? shipping = null,
        ILogger<SubscriptionService>? logger = null)
    { _db = db; _orders = orders; _shipping = shipping ?? new ShippingService(); _logger = logger; }

    private IQueryable<Subscription> Query() =>
        _db.Subscriptions.Include(s => s.ProductVariant).ThenInclude(v => v!.Product);

    private static SubscriptionDto ToDto(Subscription s) => new(s.Id, s.ProductVariantId,
        s.ProductVariant?.Product?.Name ?? string.Empty, s.ProductVariant?.Label ?? string.Empty,
        s.ProductVariant?.Price ?? 0m, s.Quantity, s.IntervalMonths, s.NextDeliveryDate, s.Status, s.DeliveryCity,
        s.DeliveryAddress, s.Courier, s.DeliveryType, s.OrdersCreated, s.LastOrderAt, DiscountPercent);

    public async Task<List<SubscriptionDto>> GetForUserAsync(string userId) =>
        (await Query().AsNoTracking().Where(s => s.UserId == userId && s.Status != Subscription.Cancelled)
            .OrderBy(s => s.NextDeliveryDate).ToListAsync()).Select(ToDto).ToList();

    public async Task<List<SubscriptionDto>> GetAllAsync() =>
        (await Query().AsNoTracking().OrderBy(s => s.Status).ThenBy(s => s.NextDeliveryDate).Take(500).ToListAsync())
        .Select(ToDto).ToList();

    public async Task<SubscriptionDto> CreateAsync(string userId, SubscriptionCreateDto dto)
    {
        var variant = await _db.ProductVariants.Include(v => v.Product).FirstOrDefaultAsync(v => v.Id == dto.ProductVariantId)
                      ?? throw new BusinessException("Опаковката не е намерена.", StatusCodes.Status404NotFound);
        if (variant.Product is null || !variant.Product.InStock || !variant.InStock)
            throw new BusinessException("Този продукт в момента не е наличен за абонамент.");
        if (variant.Product.Type == "Подаръчен комплект")
            throw new BusinessException("Подаръчните комплекти не могат да се заявяват като абонамент.");
        if (await _db.Subscriptions.CountAsync(s => s.UserId == userId && s.Status != Subscription.Cancelled) >= MaxActivePerUser)
            throw new BusinessException($"Можете да имате най-много {MaxActivePerUser} абонамента.");

        var first = (dto.FirstDeliveryDate ?? DateTime.UtcNow.AddDays(7)).Date;
        if (first <= DateTime.UtcNow.Date || first > DateTime.UtcNow.Date.AddDays(90))
            throw new BusinessException("Първата доставка трябва да е между утре и 90 дни напред.");

        var s = new Subscription
        {
            UserId = userId, ProductVariantId = variant.Id, Quantity = dto.Quantity, IntervalMonths = dto.IntervalMonths,
            NextDeliveryDate = first, Status = Subscription.Active,
            CustomerName = dto.CustomerName.Trim(), CustomerPhone = dto.CustomerPhone.Trim(),
            CustomerEmail = dto.CustomerEmail?.Trim() ?? string.Empty, DeliveryCity = dto.DeliveryCity.Trim(),
            DeliveryAddress = dto.DeliveryAddress.Trim(),
            Courier = _shipping.NormalizeCourier(dto.Courier), DeliveryType = _shipping.NormalizeDeliveryType(dto.DeliveryType)
        };
        _db.Subscriptions.Add(s);
        await _db.SaveChangesAsync();
        s.ProductVariant = variant;
        return ToDto(s);
    }

    public async Task<SubscriptionDto> SetStatusAsync(string userId, int id, string action)
    {
        var s = await Query().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId)
                ?? throw new BusinessException("Абонаментът не е намерен.", StatusCodes.Status404NotFound);
        if (s.Status == Subscription.Cancelled)
            throw new BusinessException("Абонаментът е прекратен.", StatusCodes.Status409Conflict);

        switch (action.Trim().ToLowerInvariant())
        {
            case "pause": s.Status = Subscription.Paused; break;
            case "resume":
                s.Status = Subscription.Active;
                if (s.NextDeliveryDate <= DateTime.UtcNow.Date) s.NextDeliveryDate = DateTime.UtcNow.Date.AddDays(3);
                break;
            case "cancel": s.Status = Subscription.Cancelled; break;
            default: throw new BusinessException("Невалидно действие. Позволени: pause, resume, cancel.");
        }
        await _db.SaveChangesAsync();
        return ToDto(s);
    }

    public async Task<int> ProcessDueAsync(DateTime nowUtc)
    {
        var due = await Query().Where(s => s.Status == Subscription.Active && s.NextDeliveryDate <= nowUtc)
            .OrderBy(s => s.NextDeliveryDate).Take(100).ToListAsync();
        var created = 0;

        foreach (var s in due)
        {
            try
            {
                var dto = new CreateOrderDto
                {
                    CustomerName = s.CustomerName, CustomerPhone = s.CustomerPhone,
                    CustomerEmail = string.IsNullOrWhiteSpace(s.CustomerEmail) ? null : s.CustomerEmail,
                    DeliveryCity = s.DeliveryCity, DeliveryAddress = s.DeliveryAddress,
                    Courier = s.Courier, DeliveryType = s.DeliveryType,
                    PaymentType = "cod", PaymentMethod = "Наложен платеж (абонамент)",
                    Notes = $"Автоматична поръчка по абонамент №{s.Id}",
                    Items = { new CreateOrderItemDto { ProductVariantId = s.ProductVariantId, Quantity = s.Quantity } }
                };
                await _orders.CreateAsync(dto, new OrderContext(s.UserId, s.Id, DiscountPercent));
                // при колизия на номера поръчките чистят ChangeTracker-а – връщаме абонамента, за да се запишат промените му
                if (_db.Entry(s).State == EntityState.Detached) _db.Attach(s);

                s.LastOrderAt = nowUtc;
                s.OrdersCreated++;
                var next = s.NextDeliveryDate.AddMonths(s.IntervalMonths);
                while (next <= nowUtc) next = next.AddMonths(s.IntervalMonths); // след дълъг престой не създаваме натрупани поръчки
                s.NextDeliveryDate = next;
                created++;
            }
            catch (BusinessException ex)
            {
                // напр. изчерпана наличност – опитваме отново след 3 дни
                _logger?.LogWarning("Абонамент {Id}: поръчката не е създадена ({Reason}). Нов опит след 3 дни.", s.Id, ex.Message);
                if (_db.Entry(s).State == EntityState.Detached) _db.Attach(s);
                s.NextDeliveryDate = nowUtc.Date.AddDays(3);
            }
            await _db.SaveChangesAsync();
        }
        return created;
    }
}

/// <summary>Проверява на всеки час дали има абонаменти за доставка.</summary>
public class SubscriptionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<SubscriptionBackgroundService> _logger;
    public SubscriptionBackgroundService(IServiceScopeFactory scopes, ILogger<SubscriptionBackgroundService> logger)
    { _scopes = scopes; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // изчакваме приложението да стартира
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var n = await scope.ServiceProvider.GetRequiredService<ISubscriptionService>().ProcessDueAsync(DateTime.UtcNow);
                if (n > 0) _logger.LogInformation("Създадени са {Count} поръчки по абонаменти.", n);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Грешка при обработка на абонаментите");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
