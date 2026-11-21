using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

/// <summary>Параметри на заявка към каталога. Page = null връща всички резултати без странициране.</summary>
public record ProductQuery(string? Type = null, string? Region = null, string? Category = null, string? Search = null,
    decimal? MinPrice = null, decimal? MaxPrice = null, int? BeekeeperId = null, bool InStockOnly = false,
    string? Sort = null, int? Page = null, int PageSize = 12, bool IncludeHidden = false);

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(string? type, string? region, string? category, string? search);
    Task<(List<ProductDto> Items, int Total)> QueryAsync(ProductQuery query);
    Task<ProductDto> GetByIdAsync(int id);
    Task<List<ProductDto>> GetByBatchAsync(string batchNumber);
    Task<int?> GetBeekeeperIdAsync(int productId);
    Task<ProductDto> CreateAsync(ProductUpsertDto dto);
    Task<ProductDto> UpdateAsync(int id, ProductUpsertDto dto);
    Task DeleteAsync(int id);
    Task<List<InventoryItemDto>> GetInventoryAsync(int? beekeeperId, bool lowOnly);
    Task<InventoryItemDto> SetStockAsync(int variantId, int quantity, int? beekeeperId);
}

public class ProductService : IProductService
{
    public const int LowStockThreshold = 10;
    public const int DefaultStock = 100;
    public static readonly string[] Types = { "Мед", "Пчелен прашец", "Подаръчен комплект" };

    private readonly ApplicationDbContext _db;
    private readonly IStockAlertService? _alerts;
    public ProductService(ApplicationDbContext db, IStockAlertService? alerts = null) { _db = db; _alerts = alerts; }

    private IQueryable<Product> Query() =>
        _db.Products.AsNoTracking()
            .Include(p => p.Beekeeper).ThenInclude(b => b!.Region)
            .Include(p => p.Variants)
            .AsSplitQuery();

    public async Task<List<ProductDto>> GetAllAsync(string? type, string? region, string? category, string? search) =>
        (await QueryAsync(new ProductQuery(type, region, category, search))).Items;

    public async Task<(List<ProductDto> Items, int Total)> QueryAsync(ProductQuery f)
    {
        var q = Query();

        // продуктите на скрити пчелини не се показват в публичния каталог
        if (!f.IncludeHidden)
            q = q.Where(p => p.Beekeeper == null || p.Beekeeper.IsActive);

        if (!string.IsNullOrWhiteSpace(f.Type) && f.Type != "all")
            q = q.Where(p => p.Type == f.Type);

        if (!string.IsNullOrWhiteSpace(f.Region) && f.Region != "all")
        {
            var r = f.Region.Trim();
            q = q.Where(p => (p.Beekeeper != null && p.Beekeeper.Region!.Name == r) || p.Origin.StartsWith(r));
        }

        if (!string.IsNullOrWhiteSpace(f.Category) && f.Category != "all")
            q = q.Where(p => p.Category == f.Category);

        if (f.BeekeeperId.HasValue)
            q = q.Where(p => p.BeekeeperId == f.BeekeeperId.Value);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim();
            q = q.Where(p => p.Name.Contains(s) || p.NameEn.Contains(s) || p.Origin.Contains(s) || p.Tag.Contains(s) ||
                             (p.Beekeeper != null && (p.Beekeeper.ApiaryName.Contains(s) || p.Beekeeper.FullName.Contains(s))));
        }

        // Цените се сравняват като double – така заявката работи еднакво на SQL Server и SQLite
        if (f.MinPrice.HasValue)
        {
            var min = (double)f.MinPrice.Value;
            q = q.Where(p => p.Variants.Any(v => (double)v.Price >= min));
        }
        if (f.MaxPrice.HasValue)
        {
            var max = (double)f.MaxPrice.Value;
            q = q.Where(p => p.Variants.Any(v => (double)v.Price <= max));
        }

        if (f.InStockOnly)
            q = q.Where(p => p.InStock && p.Variants.Any(v => v.InStock && v.StockQuantity > 0));

        var total = await q.CountAsync();

        IOrderedQueryable<Product> ordered = (f.Sort ?? "rating").ToLowerInvariant() switch
        {
            "price-asc" => q.OrderBy(p => p.Variants.Min(v => (double?)v.Price)),
            "price-desc" => q.OrderByDescending(p => p.Variants.Min(v => (double?)v.Price)),
            "name" => q.OrderBy(p => p.Name),
            "new" => q.OrderByDescending(p => p.CreatedAt),
            _ => q.OrderByDescending(p => p.Rating)
        };
        var sorted = ordered.ThenBy(p => p.Id).AsQueryable();

        if (f.Page.HasValue)
        {
            var size = Math.Clamp(f.PageSize, 1, 100);
            sorted = sorted.Skip((Math.Max(1, f.Page.Value) - 1) * size).Take(size);
        }

        var list = await sorted.ToListAsync();
        return (list.Select(p => p.ToDto()).ToList(), total);
    }

    public async Task<ProductDto> GetByIdAsync(int id)
    {
        var p = await Query().FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException("Продуктът не е намерен.", StatusCodes.Status404NotFound);
        return p.ToDto();
    }

    public async Task<List<ProductDto>> GetByBatchAsync(string batchNumber)
    {
        var b = (batchNumber ?? string.Empty).Trim();
        if (b.Length == 0) return new();
        return (await Query().Where(p => p.BatchNumber == b).ToListAsync()).Select(p => p.ToDto()).ToList();
    }

    public async Task<int?> GetBeekeeperIdAsync(int productId) =>
        await _db.Products.AsNoTracking().Where(p => p.Id == productId).Select(p => (int?)p.BeekeeperId).FirstOrDefaultAsync();

    public async Task<ProductDto> CreateAsync(ProductUpsertDto dto)
    {
        Validate(dto);
        if (!await _db.Beekeepers.AnyAsync(b => b.Id == dto.BeekeeperId))
            throw new BusinessException("Невалиден идентификатор на пчелар.");

        var p = new Product();
        Apply(p, dto);
        foreach (var v in dto.Variants) p.Variants.Add(ToVariant(v));
        _db.Products.Add(p);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(p.Id);
    }

    public async Task<ProductDto> UpdateAsync(int id, ProductUpsertDto dto)
    {
        Validate(dto);
        var p = await _db.Products.Include(x => x.Variants).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException("Продуктът не е намерен.", StatusCodes.Status404NotFound);
        if (!await _db.Beekeepers.AnyAsync(b => b.Id == dto.BeekeeperId))
            throw new BusinessException("Невалиден идентификатор на пчелар.");

        var wasAvailable = p.InStock && p.Variants.Any(v => v.InStock && v.StockQuantity > 0);
        Apply(p, dto);

        // Опаковки, които вече са част от поръчки, не се трият, а само се обновяват по Label
        var existing = p.Variants.ToList();
        foreach (var v in dto.Variants)
        {
            var match = existing.FirstOrDefault(e => e.Label == v.Label);
            if (match != null)
            {
                match.Weight = v.Weight; match.Price = v.Price;
                if (v.StockQuantity.HasValue) match.StockQuantity = v.StockQuantity.Value;
                match.InStock = v.InStock && match.StockQuantity > 0;
                existing.Remove(match);
            }
            else p.Variants.Add(ToVariant(v));
        }
        foreach (var removed in existing)
        {
            if (await _db.OrderItems.AnyAsync(i => i.ProductVariantId == removed.Id))
                removed.InStock = false; // пази историята на поръчките
            else
                _db.ProductVariants.Remove(removed);
        }

        await _db.SaveChangesAsync();
        if (!wasAvailable && p.InStock && p.Variants.Any(v => v.InStock && v.StockQuantity > 0) && _alerts != null)
            await _alerts.NotifyAvailableAsync(id);
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var p = await _db.Products.FindAsync(id)
                ?? throw new BusinessException("Продуктът не е намерен.", StatusCodes.Status404NotFound);
        if (await _db.OrderItems.AnyAsync(i => i.ProductId == id))
        {
            // Има история на поръчки – само скриваме продукта от наличност
            p.InStock = false;
        }
        else
        {
            _db.Products.Remove(p); // отзивите към продукта остават (ProductId = NULL)
        }
        await _db.SaveChangesAsync();
    }

    // ---------- Наличности ----------
    public async Task<List<InventoryItemDto>> GetInventoryAsync(int? beekeeperId, bool lowOnly)
    {
        var q = _db.ProductVariants.AsNoTracking().AsQueryable();
        if (beekeeperId.HasValue) q = q.Where(v => v.Product!.BeekeeperId == beekeeperId.Value);
        if (lowOnly) q = q.Where(v => v.StockQuantity <= LowStockThreshold);

        return await q.OrderBy(v => v.StockQuantity).ThenBy(v => v.Product!.Name)
            .Select(v => new InventoryItemDto(v.Id, v.ProductId, v.Product!.Name, v.Label,
                v.Product.Beekeeper != null ? v.Product.Beekeeper.ApiaryName : string.Empty,
                v.StockQuantity, v.InStock, v.StockQuantity <= LowStockThreshold))
            .ToListAsync();
    }

    public async Task<InventoryItemDto> SetStockAsync(int variantId, int quantity, int? beekeeperId)
    {
        var v = await _db.ProductVariants.Include(x => x.Product).ThenInclude(p => p!.Beekeeper)
                    .FirstOrDefaultAsync(x => x.Id == variantId)
                ?? throw new BusinessException("Опаковката не е намерена.", StatusCodes.Status404NotFound);
        if (beekeeperId.HasValue && v.Product!.BeekeeperId != beekeeperId.Value)
            throw new BusinessException("Нямате право да променяте този продукт.", StatusCodes.Status403Forbidden);

        var wasEmpty = !v.InStock || v.StockQuantity <= 0;
        v.StockQuantity = quantity;
        v.InStock = quantity > 0;
        await _db.SaveChangesAsync();
        if (wasEmpty && quantity > 0 && v.Product!.InStock && _alerts != null)
            await _alerts.NotifyAvailableAsync(v.ProductId);
        return new InventoryItemDto(v.Id, v.ProductId, v.Product!.Name, v.Label, v.Product.Beekeeper?.ApiaryName ?? string.Empty,
            v.StockQuantity, v.InStock, v.StockQuantity <= LowStockThreshold);
    }

    // ---------- помощни ----------
    private static void Validate(ProductUpsertDto d)
    {
        if (!Types.Contains(d.Type))
            throw new BusinessException("Невалиден тип продукт. Позволени: " + string.Join(", ", Types) + ".");
        UrlRules.Require(d.Image, "Снимка");
        UrlRules.Require(d.LabReportUrl, "Лабораторен протокол");
    }

    private static void Apply(Product p, ProductUpsertDto d)
    {
        p.BeekeeperId = d.BeekeeperId; p.Name = d.Name; p.Type = d.Type; p.Category = d.Category;
        p.Origin = d.Origin; p.Description = d.Description; p.HarvestDate = d.HarvestDate; p.Purity = d.Purity;
        p.TastingNotes = d.TastingNotes; p.Color = d.Color; p.Aroma = d.Aroma; p.Crystallization = d.Crystallization;
        p.Image = d.Image; p.Tag = d.Tag; p.QuizTags = d.QuizTags; p.InStock = d.InStock;
        p.Contents = d.Contents; p.BatchNumber = d.BatchNumber; p.HarvestYear = d.HarvestYear;
        p.LabReportUrl = d.LabReportUrl; p.LabSummary = d.LabSummary; p.Certificates = d.Certificates;
        p.NameEn = d.NameEn; p.DescriptionEn = d.DescriptionEn;
    }

    private static ProductVariant ToVariant(VariantUpsertDto v)
    {
        var stock = v.StockQuantity ?? DefaultStock;
        return new() { Label = v.Label, Weight = v.Weight, Price = v.Price, StockQuantity = stock, InStock = v.InStock && stock > 0 };
    }
}
