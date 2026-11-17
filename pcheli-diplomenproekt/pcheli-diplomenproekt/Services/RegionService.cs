using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;

namespace pcheli_diplomenproekt.Services;

public interface IRegionService
{
    Task<List<RegionDto>> GetAllAsync();
}

public class RegionService : IRegionService
{
    private readonly ApplicationDbContext _db;
    public RegionService(ApplicationDbContext db) => _db = db;

    public Task<List<RegionDto>> GetAllAsync() =>
        _db.Regions.AsNoTracking().OrderBy(r => r.Id)
            .Select(r => new RegionDto(r.Id, r.Name, r.Description, r.Beekeepers.Count))
            .ToListAsync();
}
