using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

public interface IBeekeeperService
{
    Task<List<BeekeeperDto>> GetAllAsync(string? region);
    Task<BeekeeperDto> GetByIdAsync(int id, bool includeHidden = false);
    Task<BeekeeperDetailDto> GetDetailAsync(int id, bool includeHidden = false);
    Task SubmitApplicationAsync(BeekeeperApplyDto dto, string? userId = null);
    Task<List<BeekeeperApplicationDto>> GetApplicationsAsync();
    Task<BeekeeperDto> ApproveAsync(int applicationId);
    Task RejectAsync(int applicationId);

    // Личен панел на пчелара
    Task<int?> GetIdByUserAsync(string userId);
    Task<BeekeeperDto> UpdateProfileAsync(int beekeeperId, BeekeeperProfileUpdateDto dto);
    Task<BeekeeperDto> LinkAccountAsync(int beekeeperId, string email);

    // Администрация: пълно управление на пчеларите
    Task<List<AdminBeekeeperDto>> GetAllForAdminAsync();
    Task<AdminBeekeeperDto> AdminUpdateAsync(int id, BeekeeperAdminUpsertDto dto);
    Task<AdminBeekeeperDto> SetActiveAsync(int id, bool active);
    Task DeleteAsync(int id);
}

public class BeekeeperService : IBeekeeperService
{
    public const string Pending = "В обработка";
    public const string Approved = "Одобрен";
    public const string Rejected = "Отхвърлен";

    private readonly ApplicationDbContext _db;
    private readonly INotificationService? _notify;
    private readonly UserManager<ApplicationUser>? _users;

    public BeekeeperService(ApplicationDbContext db, INotificationService? notify = null, UserManager<ApplicationUser>? users = null)
    { _db = db; _notify = notify; _users = users; }

    // Проекция директно в SQL заявката (регионът и броят продукти идват с JOIN / подзаявка)
    private static IQueryable<BeekeeperDto> Project(IQueryable<Beekeeper> q) => q.Select(b => new BeekeeperDto(
        b.Id, b.FullName, b.ApiaryName, b.RegistrationNumber, b.Region!.Name, b.CityOrVillage, b.Phone, b.Email, b.Bio,
        b.ExperienceYears, b.HiveCount, b.Rating, b.IsVerified, b.HasHoney, b.HasPollen, b.Products.Count, b.PhotoUrl));

    public async Task<List<BeekeeperDto>> GetAllAsync(string? region)
    {
        var q = _db.Beekeepers.AsNoTracking().Where(b => b.IsActive);
        if (!string.IsNullOrWhiteSpace(region) && region != "all")
            q = q.Where(b => b.Region!.Name == region.Trim());

        return await Project(q.OrderByDescending(b => b.Rating).ThenBy(b => b.Id)).ToListAsync();
    }

    public async Task<BeekeeperDto> GetByIdAsync(int id, bool includeHidden = false) =>
        await Project(_db.Beekeepers.AsNoTracking().Where(b => b.Id == id && (includeHidden || b.IsActive))).FirstOrDefaultAsync()
        ?? throw new BusinessException("Пчеларят не е намерен.", StatusCodes.Status404NotFound);

    public async Task<BeekeeperDetailDto> GetDetailAsync(int id, bool includeHidden = false)
    {
        var dto = await GetByIdAsync(id, includeHidden);
        var extra = await _db.Beekeepers.AsNoTracking().Where(b => b.Id == id)
            .Select(b => new { b.Story, b.ApiaryPhotos }).FirstAsync();

        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Beekeeper).ThenInclude(b => b!.Region).Include(p => p.Variants)
            .Where(p => p.BeekeeperId == id).OrderByDescending(p => p.Rating).ThenBy(p => p.Id)
            .AsSplitQuery().ToListAsync();

        var reviews = await _db.Reviews.AsNoTracking().Include(r => r.Product)
            .Where(r => r.IsApproved && r.Product != null && r.Product.BeekeeperId == id)
            .OrderByDescending(r => r.CreatedAt).Take(30).ToListAsync();

        var avg = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 2) : dto.Rating;
        return new BeekeeperDetailDto(dto, extra.Story,
            extra.ApiaryPhotos.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            products.Select(p => p.ToDto()).ToList(),
            reviews.Select(r => new BeekeeperReviewDto(r.Id, r.AuthorName, r.City, r.Rating, r.Comment, r.CreatedAt, r.Product?.Name)).ToList(),
            avg, reviews.Count);
    }

    public async Task SubmitApplicationAsync(BeekeeperApplyDto dto, string? userId = null)
    {
        var regionName = dto.Region.Trim();
        var region = await _db.Regions.FirstOrDefaultAsync(r => r.Name == regionName)
                     ?? throw new BusinessException("Невалиден регион. Изберете регион от списъка.");

        var reg = dto.RegistrationNumber.Trim();
        if (await _db.Beekeepers.AnyAsync(b => b.RegistrationNumber == reg))
            throw new BusinessException("Пчелар с този БАБХ номер вече е регистриран.", StatusCodes.Status409Conflict);
        if (await _db.BeekeeperApplications.AnyAsync(a => a.RegistrationNumber == reg && a.Status == Pending))
            throw new BusinessException("Вече има заявка с този БАБХ номер, която чака одобрение.", StatusCodes.Status409Conflict);

        _db.BeekeeperApplications.Add(new BeekeeperApplication
        {
            FullName = dto.FullName.Trim(), ApiaryName = dto.ApiaryName.Trim(), RegistrationNumber = reg,
            RegionId = region.Id, CityOrVillage = dto.CityOrVillage.Trim(), Phone = dto.Phone.Trim(),
            Email = dto.Email?.Trim(), OffersHoney = dto.OffersHoney, OffersPollen = dto.OffersPollen,
            ProductDetails = dto.ProductDetails?.Trim() ?? string.Empty, HiveCount = dto.HiveCount,
            Status = Pending, SubmittedAt = DateTime.UtcNow, UserId = userId
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<BeekeeperApplicationDto>> GetApplicationsAsync() =>
        (await _db.BeekeeperApplications.AsNoTracking().Include(a => a.Region)
            .OrderBy(a => a.Status == Pending ? 0 : 1).ThenByDescending(a => a.SubmittedAt).ToListAsync())
        .Select(a => a.ToDto()).ToList();

    public async Task<BeekeeperDto> ApproveAsync(int applicationId)
    {
        var app = await _db.BeekeeperApplications.Include(a => a.Region).FirstOrDefaultAsync(a => a.Id == applicationId)
                  ?? throw new BusinessException("Заявката не е намерена.", StatusCodes.Status404NotFound);
        var regionName = app.Region?.Name ?? string.Empty;
        if (app.Status != Pending)
            throw new BusinessException("Заявката вече е обработена.", StatusCodes.Status409Conflict);
        if (await _db.Beekeepers.AnyAsync(b => b.RegistrationNumber == app.RegistrationNumber))
            throw new BusinessException("Пчелар с този БАБХ номер вече съществува.", StatusCodes.Status409Conflict);

        // Акаунтът се свързва само ако не е вече свързан с друг пчелин
        var linkUser = app.UserId;
        if (linkUser != null && await _db.Beekeepers.AnyAsync(b => b.UserId == linkUser)) linkUser = null;

        await using var tx = await _db.Database.BeginTransactionAsync();

        var bk = new Beekeeper
        {
            FullName = app.FullName, ApiaryName = app.ApiaryName, RegistrationNumber = app.RegistrationNumber,
            RegionId = app.RegionId, CityOrVillage = app.CityOrVillage, Phone = app.Phone, Email = app.Email ?? string.Empty,
            Bio = $"Пчелин от регион {regionName}. Кошери: {app.HiveCount}. {app.ProductDetails}".Trim(),
            HiveCount = app.HiveCount, ExperienceYears = 0, HasHoney = app.OffersHoney, HasPollen = app.OffersPollen,
            IsVerified = true, Rating = 5.0, UserId = linkUser
        };
        _db.Beekeepers.Add(bk);
        await _db.SaveChangesAsync();

        if (app.OffersHoney)
        {
            _db.Products.Add(new Product
            {
                BeekeeperId = bk.Id, Name = $"Натурален Мед от {app.ApiaryName}", Type = "Мед", Category = "herbal",
                Origin = $"{regionName} ({app.CityOrVillage})",
                Description = $"Чист домашен пчелен мед директно от пчелин «{app.ApiaryName}». {app.ProductDetails}".Trim(),
                HarvestDate = "Реколта 2026", Purity = "100% Натурален", Image = "./images/acacia.jpg", Tag = "Нов Пчелин",
                QuizTags = "immunity,herbal",
                Variants = { new ProductVariant { Label = "Буркан 0.9 кг", Weight = "0.9kg", Price = 10.00m } }
            });
        }
        if (app.OffersPollen)
        {
            _db.Products.Add(new Product
            {
                BeekeeperId = bk.Id, Name = $"Пчелен Прашец от {app.ApiaryName}", Type = "Пчелен прашец", Category = "pollen",
                Origin = $"{regionName} ({app.CityOrVillage})",
                Description = $"Цветен пчелен прашец от региона на {app.CityOrVillage}. Натурален източник на енергия и ензими.",
                HarvestDate = "Реколта 2026", Purity = "100% Чист прашец", Image = "./images/pollen.jpg", Tag = "Нов Пчелин",
                QuizTags = "immunity,antioxidant",
                Variants = { new ProductVariant { Label = "Буркан 250 г", Weight = "0.25kg", Price = 8.50m } }
            });
        }

        app.Status = Approved;
        app.BeekeeperId = bk.Id;
        app.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        if (linkUser != null) await GrantBeekeeperRoleAsync(linkUser);
        _notify?.ApplicationDecided(app, approved: true);

        return await GetByIdAsync(bk.Id);
    }

    public async Task RejectAsync(int applicationId)
    {
        var app = await _db.BeekeeperApplications.FindAsync(applicationId)
                  ?? throw new BusinessException("Заявката не е намерена.", StatusCodes.Status404NotFound);
        if (app.Status != Pending)
            throw new BusinessException("Заявката вече е обработена.", StatusCodes.Status409Conflict);
        app.Status = Rejected;
        app.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _notify?.ApplicationDecided(app, approved: false);
    }

    // ---------- Личен панел ----------
    private async Task GrantBeekeeperRoleAsync(string userId)
    {
        if (_users is null) return;
        var user = await _users.FindByIdAsync(userId);
        if (user != null && !await _users.IsInRoleAsync(user, DbInitializer.BeekeeperRole))
            await _users.AddToRoleAsync(user, DbInitializer.BeekeeperRole);
    }

    public async Task<int?> GetIdByUserAsync(string userId) =>
        await _db.Beekeepers.AsNoTracking().Where(b => b.UserId == userId).Select(b => (int?)b.Id).FirstOrDefaultAsync();

    public async Task<BeekeeperDto> UpdateProfileAsync(int beekeeperId, BeekeeperProfileUpdateDto dto)
    {
        UrlRules.Require(dto.PhotoUrl, "Снимка на пчеларя");
        foreach (var p in dto.ApiaryPhotos) UrlRules.Require(p, "Снимка на пчелина");
        if (dto.ApiaryPhotos.Any(p => p.Contains(';')))
            throw new BusinessException("Адресите на снимките не могат да съдържат „;“.");

        var b = await _db.Beekeepers.FirstOrDefaultAsync(x => x.Id == beekeeperId)
                ?? throw new BusinessException("Пчеларят не е намерен.", StatusCodes.Status404NotFound);

        if (!string.IsNullOrWhiteSpace(dto.Phone)) b.Phone = dto.Phone.Trim();
        if (dto.Email != null) b.Email = dto.Email.Trim();
        b.Bio = dto.Bio.Trim();
        b.Story = dto.Story.Trim();
        b.PhotoUrl = dto.PhotoUrl.Trim();
        b.ApiaryPhotos = string.Join(';', dto.ApiaryPhotos.Select(p => p.Trim()).Where(p => p.Length > 0));
        await _db.SaveChangesAsync();
        return await GetByIdAsync(beekeeperId, includeHidden: true);
    }

    public async Task<BeekeeperDto> LinkAccountAsync(int beekeeperId, string email)
    {
        if (_users is null) throw new BusinessException("Управлението на акаунти не е налично.");
        var b = await _db.Beekeepers.FirstOrDefaultAsync(x => x.Id == beekeeperId)
                ?? throw new BusinessException("Пчеларят не е намерен.", StatusCodes.Status404NotFound);
        var user = await _users.FindByEmailAsync(email.Trim())
                   ?? throw new BusinessException("Няма регистриран потребител с този имейл. Той трябва първо да си направи акаунт.", StatusCodes.Status404NotFound);
        if (await _db.Beekeepers.AnyAsync(x => x.UserId == user.Id && x.Id != beekeeperId))
            throw new BusinessException("Този акаунт вече е свързан с друг пчелин.", StatusCodes.Status409Conflict);

        b.UserId = user.Id;
        await _db.SaveChangesAsync();
        await GrantBeekeeperRoleAsync(user.Id);
        return await GetByIdAsync(beekeeperId, includeHidden: true);
    }

    // ---------- Администрация ----------
    private IQueryable<AdminBeekeeperDto> AdminProject(IQueryable<Beekeeper> q) => q.Select(b => new AdminBeekeeperDto(
        b.Id, b.FullName, b.ApiaryName, b.RegistrationNumber, b.RegionId, b.Region!.Name, b.CityOrVillage, b.Phone, b.Email,
        b.Bio, b.ExperienceYears, b.HiveCount, b.IsVerified, b.HasHoney, b.HasPollen, b.IsActive, b.UserId, b.Products.Count));

    private async Task<AdminBeekeeperDto> GetAdminAsync(int id) =>
        await AdminProject(_db.Beekeepers.AsNoTracking().Where(b => b.Id == id)).FirstOrDefaultAsync()
        ?? throw new BusinessException("Пчеларят не е намерен.", StatusCodes.Status404NotFound);

    public async Task<List<AdminBeekeeperDto>> GetAllForAdminAsync() =>
        await AdminProject(_db.Beekeepers.AsNoTracking().OrderBy(b => b.ApiaryName)).ToListAsync();

    public async Task<AdminBeekeeperDto> AdminUpdateAsync(int id, BeekeeperAdminUpsertDto dto)
    {
        var b = await _db.Beekeepers.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException("Пчеларят не е намерен.", StatusCodes.Status404NotFound);
        var regionName = dto.Region.Trim();
        var region = await _db.Regions.FirstOrDefaultAsync(r => r.Name == regionName)
                     ?? throw new BusinessException("Невалиден регион. Изберете регион от списъка.");
        var reg = dto.RegistrationNumber.Trim();
        if (await _db.Beekeepers.AnyAsync(x => x.RegistrationNumber == reg && x.Id != id))
            throw new BusinessException("Друг пчелар вече има този БАБХ номер.", StatusCodes.Status409Conflict);

        b.FullName = dto.FullName.Trim(); b.ApiaryName = dto.ApiaryName.Trim(); b.RegistrationNumber = reg;
        b.RegionId = region.Id; b.CityOrVillage = dto.CityOrVillage.Trim(); b.Phone = dto.Phone.Trim();
        b.Email = dto.Email?.Trim() ?? string.Empty; b.Bio = dto.Bio.Trim();
        b.ExperienceYears = dto.ExperienceYears; b.HiveCount = dto.HiveCount;
        b.IsVerified = dto.IsVerified; b.HasHoney = dto.HasHoney; b.HasPollen = dto.HasPollen; b.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return await GetAdminAsync(id);
    }

    public async Task<AdminBeekeeperDto> SetActiveAsync(int id, bool active)
    {
        var b = await _db.Beekeepers.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException("Пчеларят не е намерен.", StatusCodes.Status404NotFound);
        b.IsActive = active;
        await _db.SaveChangesAsync();
        return await GetAdminAsync(id);
    }

    /// <summary>Трие пчелин само ако няма история на поръчки; иначе се предлага скриване (пази счетоводните данни).</summary>
    public async Task DeleteAsync(int id)
    {
        var b = await _db.Beekeepers.Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException("Пчеларят не е намерен.", StatusCodes.Status404NotFound);
        var productIds = b.Products.Select(p => p.Id).ToList();
        if (await _db.OrderItems.AnyAsync(i => productIds.Contains(i.ProductId)) ||
            await _db.Subscriptions.AnyAsync(s => productIds.Contains(s.ProductVariant!.ProductId)))
            throw new BusinessException("Пчеларят има поръчки или абонаменти и не може да бъде изтрит. Скрийте го от сайта.", StatusCodes.Status409Conflict);

        // заявката за регистрация остава в архива (BeekeeperId става NULL), акаунтът се отвързва автоматично
        _db.Beekeepers.Remove(b);
        await _db.SaveChangesAsync();
    }
}
