using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BeekeepersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BeekeepersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/beekeepers
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? region = null)
    {
        var query = _context.Beekeepers.Include(b => b.Products).AsQueryable();

        if (!string.IsNullOrWhiteSpace(region) && region != "all")
        {
            query = query.Where(b => b.Region.ToLower().Contains(region.ToLower()));
        }

        var list = await query
            .OrderByDescending(b => b.Rating)
            .Select(b => new
            {
                b.Id,
                b.FullName,
                b.ApiaryName,
                b.RegistrationNumber,
                b.Region,
                b.CityOrVillage,
                b.Phone,
                b.Email,
                b.Bio,
                b.ExperienceYears,
                b.HiveCount,
                b.Rating,
                b.IsVerified,
                b.HasHoney,
                b.HasPollen,
                ProductCount = b.Products.Count
            })
            .ToListAsync();

        return Ok(list);
    }

    // GET: api/beekeepers/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var beekeeper = await _context.Beekeepers
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (beekeeper == null) return NotFound(new { message = "Пчеларят не е намерен." });

        return Ok(beekeeper);
    }

    // POST: api/beekeepers/apply
    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] BeekeeperApplication app)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        app.SubmittedAt = DateTime.UtcNow;
        app.Status = "Одобрен"; // За целите на демонстрацията директно добавяме и като пчелар

        _context.BeekeeperApplications.Add(app);
        await _context.SaveChangesAsync();

        // Автоматично регистрираме пчеларя за демонстрацията
        var newBeekeeper = new Beekeeper
        {
            FullName = app.FullName,
            ApiaryName = app.ApiaryName,
            RegistrationNumber = app.RegistrationNumber,
            Region = app.Region,
            CityOrVillage = app.CityOrVillage,
            Phone = app.Phone,
            Email = app.Email ?? "",
            Bio = $"Пчелин от регион {app.Region}. Кошери: {app.HiveCount}. {app.ProductDetails}",
            HiveCount = app.HiveCount,
            HasHoney = app.OffersHoney,
            HasPollen = app.OffersPollen,
            IsVerified = true,
            Rating = 5.0
        };

        _context.Beekeepers.Add(newBeekeeper);
        await _context.SaveChangesAsync();

        // Добавяме и първоначален продукт според това какво предлага пчеларят
        if (app.OffersHoney)
        {
            _context.Products.Add(new Product
            {
                BeekeeperId = newBeekeeper.Id,
                Name = $"Натурален Мед от {app.ApiaryName}",
                Type = "Мед",
                Category = "herbal",
                Origin = $"{app.Region} ({app.CityOrVillage})",
                Description = $"Чист домашен пчелен мед директно от пчелин «{app.ApiaryName}». {app.ProductDetails}",
                HarvestDate = "Реколта 2026",
                Purity = "100% Натурален",
                Price = 10.00m,
                Weight = "0.9 кг",
                Image = "./images/acacia.jpg",
                Tag = "Нов Пчелин",
                Rating = 5.0
            });
        }

        if (app.OffersPollen)
        {
            _context.Products.Add(new Product
            {
                BeekeeperId = newBeekeeper.Id,
                Name = $"Пчелен Прашец от {app.ApiaryName}",
                Type = "Пчелен прашец",
                Category = "pollen_dried",
                Origin = $"{app.Region} ({app.CityOrVillage})",
                Description = $"Цветен пчелен прашец от региона на {app.CityOrVillage}. Натурален източник на енергия и ензими.",
                HarvestDate = "Реколта 2026",
                Purity = "100% Чист прашец",
                Price = 8.50m,
                Weight = "250 г",
                Image = "./images/pollen.jpg",
                Tag = "Нов Пчелин",
                Rating = 5.0
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Успешно се присъединихте към «Пчелари България»! Вашият пчелин и продукти вече са видими в платформата.",
            beekeeperId = newBeekeeper.Id
        });
    }
}
