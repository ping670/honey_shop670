using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;

namespace pcheli_diplomenproekt.Services;

public interface IRegionService
{
    Task<List<RegionDto>> GetAllAsync();
    Task<RegionDto> CreateAsync(RegionUpsertDto dto);
    Task<RegionDto> UpdateAsync(int id, RegionUpsertDto dto);
    Task DeleteAsync(int id);
}

public class RegionService : IRegionService
{
    private readonly ApplicationDbContext _db;
    public RegionService(ApplicationDbContext db) => _db = db;

    public Task<List<RegionDto>> GetAllAsync() =>
        _db.Regions.AsNoTracking().OrderBy(r => r.Id)
            .Select(r => new RegionDto(r.Id, r.Name, r.Description, r.Beekeepers.Count(b => b.IsActive)))
            .ToListAsync();

    public Task<RegionDto> CreateAsync(RegionUpsertDto dto) => RegionAdmin.CreateAsync(_db, dto);
    public Task<RegionDto> UpdateAsync(int id, RegionUpsertDto dto) => RegionAdmin.UpdateAsync(_db, id, dto);
    public Task DeleteAsync(int id) => RegionAdmin.DeleteAsync(_db, id);
}
