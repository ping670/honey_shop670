using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

public interface IBeekeeperService
{
    Task<List<BeekeeperDto>> GetAllAsync(string? region);
    Task<BeekeeperDto> GetByIdAsync(int id);
    Task SubmitApplicationAsync(BeekeeperApplyDto dto);
    Task<List<BeekeeperApplicationDto>> GetApplicationsAsync();
    Task<BeekeeperDto> ApproveAsync(int applicationId);
    Task RejectAsync(int applicationId);
}

public class BeekeeperService : IBeekeeperService
{
    public const string Pending = "В обработка";
    public const string Approved = "Одобрен";
    public const string Rejected = "Отхвърлен";

    private readonly ApplicationDbContext _db;
    public BeekeeperService(ApplicationDbContext db) => _db = db;

    // Проекция директно в SQL заявката (регионът и броят продукти идват с JOIN / подзаявка)
    private static IQueryable<BeekeeperDto> Project(IQueryable<Beekeeper> q) => q.Select(b => new BeekeeperDto(
        b.Id, b.FullName, b.ApiaryName, b.RegistrationNumber, b.Region!.Name, b.CityOrVillage, b.Phone, b.Email, b.Bio,
        b.ExperienceYears, b.HiveCount, b.Rating, b.IsVerified, b.HasHoney, b.HasPollen, b.Products.Count));

    public async Task<List<BeekeeperDto>> GetAllAsync(string? region)
    {
        var q = _db.Beekeepers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(region) && region != "all")
            q = q.Where(b => b.Region!.Name == region.Trim());

        return await Project(q.OrderByDescending(b => b.Rating).ThenBy(b => b.Id)).ToListAsync();
    }

    public async Task<BeekeeperDto> GetByIdAsync(int id) =>
        await Project(_db.Beekeepers.AsNoTracking().Where(b => b.Id == id)).FirstOrDefaultAsync()
        ?? throw new BusinessException("Пчеларят не е намерен.", StatusCodes.Status404NotFound);

    public async Task SubmitApplicationAsync(BeekeeperApplyDto dto)
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
            Status = Pending, SubmittedAt = DateTime.UtcNow
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

        await using var tx = await _db.Database.BeginTransactionAsync();

        var bk = new Beekeeper
        {
            FullName = app.FullName, ApiaryName = app.ApiaryName, RegistrationNumber = app.RegistrationNumber,
            RegionId = app.RegionId, CityOrVillage = app.CityOrVillage, Phone = app.Phone, Email = app.Email ?? string.Empty,
            Bio = $"Пчелин от регион {regionName}. Кошери: {app.HiveCount}. {app.ProductDetails}".Trim(),
            HiveCount = app.HiveCount, ExperienceYears = 0, HasHoney = app.OffersHoney, HasPollen = app.OffersPollen,
            IsVerified = true, Rating = 5.0
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
    }
}
