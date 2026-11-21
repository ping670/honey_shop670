using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

public interface IAccountService
{
    Task<List<AddressDto>> GetAddressesAsync(string userId);
    Task<AddressDto> AddAddressAsync(string userId, AddressUpsertDto dto);
    Task<AddressDto> UpdateAddressAsync(string userId, int id, AddressUpsertDto dto);
    Task DeleteAddressAsync(string userId, int id);

    Task<List<ProductDto>> GetWishlistAsync(string userId);
    Task<List<int>> GetWishlistIdsAsync(string userId);
    Task AddToWishlistAsync(string userId, int productId);
    Task RemoveFromWishlistAsync(string userId, int productId);
    Task MergeWishlistAsync(string userId, IEnumerable<int> productIds);
}

/// <summary>Запазени адреси и любими продукти на клиента. Всички операции са ограничени до собствените данни на потребителя.</summary>
public class AccountService : IAccountService
{
    public const int MaxAddresses = 10;
    public const int MaxWishlist = 200;

    private readonly ApplicationDbContext _db;
    public AccountService(ApplicationDbContext db) => _db = db;

    // ---------- Адреси ----------
    private static AddressDto ToDto(SavedAddress a) => new(a.Id, a.Label, a.FullName, a.Phone, a.City, a.Address, a.IsDefault);

    public async Task<List<AddressDto>> GetAddressesAsync(string userId) =>
        (await _db.SavedAddresses.AsNoTracking().Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.Id).ToListAsync()).Select(ToDto).ToList();

    public async Task<AddressDto> AddAddressAsync(string userId, AddressUpsertDto dto)
    {
        var existing = await _db.SavedAddresses.Where(a => a.UserId == userId).ToListAsync();
        if (existing.Count >= MaxAddresses)
            throw new BusinessException($"Можете да запазите най-много {MaxAddresses} адреса.");

        var a = new SavedAddress { UserId = userId };
        Apply(a, dto);
        a.IsDefault = dto.IsDefault || existing.Count == 0;
        if (a.IsDefault) foreach (var e in existing) e.IsDefault = false;
        _db.SavedAddresses.Add(a);
        await _db.SaveChangesAsync();
        return ToDto(a);
    }

    public async Task<AddressDto> UpdateAddressAsync(string userId, int id, AddressUpsertDto dto)
    {
        var all = await _db.SavedAddresses.Where(a => a.UserId == userId).ToListAsync();
        var a = all.FirstOrDefault(x => x.Id == id)
                ?? throw new BusinessException("Адресът не е намерен.", StatusCodes.Status404NotFound);
        Apply(a, dto);
        if (dto.IsDefault) { foreach (var e in all) e.IsDefault = e.Id == id; }
        await _db.SaveChangesAsync();
        return ToDto(a);
    }

    public async Task DeleteAddressAsync(string userId, int id)
    {
        var all = await _db.SavedAddresses.Where(a => a.UserId == userId).ToListAsync();
        var a = all.FirstOrDefault(x => x.Id == id)
                ?? throw new BusinessException("Адресът не е намерен.", StatusCodes.Status404NotFound);
        _db.SavedAddresses.Remove(a);
        // ако е изтрит основният адрес, следващият става основен
        if (a.IsDefault) { var next = all.FirstOrDefault(x => x.Id != id); if (next != null) next.IsDefault = true; }
        await _db.SaveChangesAsync();
    }

    private static void Apply(SavedAddress a, AddressUpsertDto d)
    {
        a.Label = d.Label.Trim(); a.FullName = d.FullName.Trim(); a.Phone = d.Phone.Trim();
        a.City = d.City.Trim(); a.Address = d.Address.Trim();
    }

    // ---------- Любими ----------
    public async Task<List<int>> GetWishlistIdsAsync(string userId) =>
        await _db.WishlistItems.AsNoTracking().Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt).Select(w => w.ProductId).ToListAsync();

    public async Task<List<ProductDto>> GetWishlistAsync(string userId)
    {
        var ids = await GetWishlistIdsAsync(userId);
        if (ids.Count == 0) return new();
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Beekeeper).ThenInclude(b => b!.Region).Include(p => p.Variants)
            .Where(p => ids.Contains(p.Id)).AsSplitQuery().ToListAsync();
        return ids.Select(id => products.FirstOrDefault(p => p.Id == id)).Where(p => p != null).Select(p => p!.ToDto()).ToList();
    }

    public async Task AddToWishlistAsync(string userId, int productId)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == productId))
            throw new BusinessException("Продуктът не е намерен.", StatusCodes.Status404NotFound);
        if (await _db.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId)) return; // идемпотентно
        if (await _db.WishlistItems.CountAsync(w => w.UserId == userId) >= MaxWishlist)
            throw new BusinessException("Списъкът с любими е пълен.");
        _db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveFromWishlistAsync(string userId, int productId)
    {
        await _db.WishlistItems.Where(w => w.UserId == userId && w.ProductId == productId).ExecuteDeleteAsync();
    }

    /// <summary>Слива любимите, събрани в браузъра преди вход, със списъка на акаунта.</summary>
    public async Task MergeWishlistAsync(string userId, IEnumerable<int> productIds)
    {
        var wanted = productIds.Distinct().Take(MaxWishlist).ToList();
        if (wanted.Count == 0) return;
        var valid = await _db.Products.Where(p => wanted.Contains(p.Id)).Select(p => p.Id).ToListAsync();
        var have = await _db.WishlistItems.Where(w => w.UserId == userId).Select(w => w.ProductId).ToListAsync();
        foreach (var id in valid.Except(have).Take(Math.Max(0, MaxWishlist - have.Count)))
            _db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = id });
        await _db.SaveChangesAsync();
    }
}
