using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

public interface IPromoService
{
    Task<PromoValidationDto> ValidateAsync(string code);
    Task<List<PromoCodeDto>> GetAllAsync();
    Task<PromoCodeDto> CreateAsync(PromoCodeUpsertDto dto);
    Task<PromoCodeDto> UpdateAsync(int id, PromoCodeUpsertDto dto);
    Task DeleteAsync(int id);
}

public class PromoService : IPromoService
{
    private readonly ApplicationDbContext _db;
    public PromoService(ApplicationDbContext db) => _db = db;

    private static string Normalize(string code) => code.Trim().ToUpperInvariant();

    public async Task<PromoValidationDto> ValidateAsync(string code)
    {
        var c = Normalize(code);
        var p = await _db.PromoCodes.AsNoTracking().FirstOrDefaultAsync(x => x.Code == c);
        if (p is null || !p.IsActive || (p.ValidUntil.HasValue && p.ValidUntil.Value < DateTime.UtcNow))
            throw new BusinessException("Невалиден или изтекъл промо код.", StatusCodes.Status404NotFound);
        return new PromoValidationDto(p.Code, p.DiscountPercent);
    }

    public async Task<List<PromoCodeDto>> GetAllAsync() =>
        (await _db.PromoCodes.AsNoTracking().OrderBy(p => p.Code).ToListAsync()).Select(p => p.ToDto()).ToList();

    public async Task<PromoCodeDto> CreateAsync(PromoCodeUpsertDto dto)
    {
        var code = Normalize(dto.Code);
        if (await _db.PromoCodes.AnyAsync(p => p.Code == code))
            throw new BusinessException("Промо код с това име вече съществува.", StatusCodes.Status409Conflict);
        var p = new PromoCode { Code = code, DiscountPercent = dto.DiscountPercent, IsActive = dto.IsActive, ValidUntil = dto.ValidUntil };
        _db.PromoCodes.Add(p);
        await _db.SaveChangesAsync();
        return p.ToDto();
    }

    public async Task<PromoCodeDto> UpdateAsync(int id, PromoCodeUpsertDto dto)
    {
        var p = await _db.PromoCodes.FindAsync(id)
                ?? throw new BusinessException("Промо кодът не е намерен.", StatusCodes.Status404NotFound);
        p.DiscountPercent = dto.DiscountPercent; p.IsActive = dto.IsActive; p.ValidUntil = dto.ValidUntil;
        await _db.SaveChangesAsync();
        return p.ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        var p = await _db.PromoCodes.FindAsync(id)
                ?? throw new BusinessException("Промо кодът не е намерен.", StatusCodes.Status404NotFound);
        _db.PromoCodes.Remove(p);
        await _db.SaveChangesAsync();
    }
}
