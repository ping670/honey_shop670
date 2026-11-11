using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/products?type=Мед&region=Странджа&category=manov&search=...
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? type = null,
        [FromQuery] string? region = null,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null)
    {
        var query = _context.Products.Include(p => p.Beekeeper).AsQueryable();

        if (!string.IsNullOrWhiteSpace(type) && type != "all")
        {
            query = query.Where(p => p.Type.ToLower() == type.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(region) && region != "all")
        {
            query = query.Where(p => p.Origin.ToLower().Contains(region.ToLower()) ||
                                     (p.Beekeeper != null && p.Beekeeper.Region.ToLower().Contains(region.ToLower())));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "all")
        {
            query = query.Where(p => p.Category.ToLower() == category.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) ||
                                     p.Origin.ToLower().Contains(s) ||
                                     (p.Beekeeper != null && p.Beekeeper.ApiaryName.ToLower().Contains(s)));
        }

        var list = await query
            .OrderByDescending(p => p.Rating)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Type,
                p.Category,
                p.Origin,
                p.Description,
                p.HarvestDate,
                p.Purity,
                p.TastingNotes,
                p.Price,
                p.Weight,
                p.Image,
                p.InStock,
                p.Rating,
                p.Tag,
                Beekeeper = p.Beekeeper == null ? null : new
                {
                    p.Beekeeper.Id,
                    p.Beekeeper.FullName,
                    p.Beekeeper.ApiaryName,
                    p.Beekeeper.RegistrationNumber,
                    p.Beekeeper.Region,
                    p.Beekeeper.CityOrVillage,
                    p.Beekeeper.Phone,
                    p.Beekeeper.Rating
                }
            })
            .ToListAsync();

        return Ok(list);
    }

    // GET: api/products/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _context.Products
            .Include(p => p.Beekeeper)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound(new { message = "Продуктът не е намерен." });

        return Ok(product);
    }

    // POST: api/products (Пчелар добавя нов мед или прашец)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var beekeeper = await _context.Beekeepers.FindAsync(product.BeekeeperId);
        if (beekeeper == null)
        {
            return BadRequest(new { message = "Невалиден идентификатор на пчелар." });
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
}
