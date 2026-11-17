using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(string? type, string? region, string? category, string? search);
    Task<ProductDto> GetByIdAsync(int id);
    Task<ProductDto> CreateAsync(ProductUpsertDto dto);
    Task<ProductDto> UpdateAsync(int id, ProductUpsertDto dto);
    Task DeleteAsync(int id);
}

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _db;
    public ProductService(ApplicationDbContext db) => _db = db;

    private IQueryable<Product> Query() =>
        _db.Products.AsNoTracking()
            .Include(p => p.Beekeeper).ThenInclude(b => b!.Region)
            .Include(p => p.Variants)
            .AsSplitQuery();

    public async Task<List<ProductDto>> GetAllAsync(string? type, string? region, string? category, string? search)
    {
        var q = Query();

        if (!string.IsNullOrWhiteSpace(type) && type != "all")
            q = q.Where(p => p.Type == type);

        if (!string.IsNullOrWhiteSpace(region) && region != "all")
        {
            var r = region.Trim();
            q = q.Where(p => (p.Beekeeper != null && p.Beekeeper.Region!.Name == r) || p.Origin.StartsWith(r));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "all")
            q = q.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p => p.Name.Contains(s) || p.Origin.Contains(s) ||
                             (p.Beekeeper != null && (p.Beekeeper.ApiaryName.Contains(s) || p.Beekeeper.FullName.Contains(s))));
        }

        var list = await q.OrderByDescending(p => p.Rating).ThenBy(p => p.Id).ToListAsync();
        return list.Select(p => p.ToDto()).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(int id)
    {
        var p = await Query().FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException("Продуктът не е намерен.", StatusCodes.Status404NotFound);
        return p.ToDto();
    }

    public async Task<ProductDto> CreateAsync(ProductUpsertDto dto)
    {
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
        var p = await _db.Products.Include(x => x.Variants).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException("Продуктът не е намерен.", StatusCodes.Status404NotFound);
        if (!await _db.Beekeepers.AnyAsync(b => b.Id == dto.BeekeeperId))
            throw new BusinessException("Невалиден идентификатор на пчелар.");

        Apply(p, dto);

        // Опаковки, които вече са част от поръчки, не се трият, а само се обновяват по Label
        var existing = p.Variants.ToList();
        foreach (var v in dto.Variants)
        {
            var match = existing.FirstOrDefault(e => e.Label == v.Label);
            if (match != null)
            {
                match.Weight = v.Weight; match.Price = v.Price; match.InStock = v.InStock;
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

    private static void Apply(Product p, ProductUpsertDto d)
    {
        p.BeekeeperId = d.BeekeeperId; p.Name = d.Name; p.Type = d.Type; p.Category = d.Category;
        p.Origin = d.Origin; p.Description = d.Description; p.HarvestDate = d.HarvestDate; p.Purity = d.Purity;
        p.TastingNotes = d.TastingNotes; p.Color = d.Color; p.Aroma = d.Aroma; p.Crystallization = d.Crystallization;
        p.Image = d.Image; p.Tag = d.Tag; p.QuizTags = d.QuizTags; p.InStock = d.InStock;
    }

    private static ProductVariant ToVariant(VariantUpsertDto v) =>
        new() { Label = v.Label, Weight = v.Weight, Price = v.Price, InStock = v.InStock };
}
