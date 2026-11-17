using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

public interface IReviewService
{
    Task<List<ReviewDto>> GetApprovedAsync();
    Task<List<ReviewDto>> GetAllAsync();
    Task<ReviewDto> CreateAsync(CreateReviewDto dto);
    Task<ReviewDto> SetApprovedAsync(int id, bool approved);
    Task DeleteAsync(int id);
}

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _db;
    public ReviewService(ApplicationDbContext db) => _db = db;

    private IQueryable<Review> Query() => _db.Reviews.AsNoTracking().Include(r => r.Product);

    public async Task<List<ReviewDto>> GetApprovedAsync() =>
        (await Query().Where(r => r.IsApproved).OrderByDescending(r => r.CreatedAt).Take(50).ToListAsync())
        .Select(r => r.ToDto()).ToList();

    public async Task<List<ReviewDto>> GetAllAsync() =>
        (await Query().OrderBy(r => r.IsApproved).ThenByDescending(r => r.CreatedAt).ToListAsync())
        .Select(r => r.ToDto()).ToList();

    public async Task<ReviewDto> CreateAsync(CreateReviewDto dto)
    {
        if (dto.ProductId.HasValue && !await _db.Products.AnyAsync(p => p.Id == dto.ProductId.Value))
            throw new BusinessException("Продуктът, за който пишете отзив, не съществува.");

        // Новите отзиви чакат модерация от администратор
        var r = new Review
        {
            AuthorName = dto.AuthorName.Trim(), City = dto.City?.Trim() ?? string.Empty,
            Rating = dto.Rating, Comment = dto.Comment.Trim(), ProductId = dto.ProductId,
            IsApproved = false, CreatedAt = DateTime.UtcNow
        };
        _db.Reviews.Add(r);
        await _db.SaveChangesAsync();
        return r.ToDto();
    }

    public async Task<ReviewDto> SetApprovedAsync(int id, bool approved)
    {
        var r = await _db.Reviews.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException("Отзивът не е намерен.", StatusCodes.Status404NotFound);
        r.IsApproved = approved;
        await _db.SaveChangesAsync();
        return r.ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        var r = await _db.Reviews.FindAsync(id)
                ?? throw new BusinessException("Отзивът не е намерен.", StatusCodes.Status404NotFound);
        _db.Reviews.Remove(r);
        await _db.SaveChangesAsync();
    }
}
