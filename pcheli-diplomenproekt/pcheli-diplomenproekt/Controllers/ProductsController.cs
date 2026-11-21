using Microsoft.AspNetCore.Mvc;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    public ProductsController(IProductService products) => _products = products;

    /// <summary>
    /// GET: api/products?type=Мед&amp;region=Странджа&amp;category=manov&amp;search=...&amp;minPrice=5&amp;maxPrice=12&amp;beekeeperId=3
    /// &amp;inStock=true&amp;sort=price-asc|price-desc|name|new|rating&amp;page=1&amp;pageSize=12.
    /// Без „page“ се връщат всички резултати. Общият брой е в заглавката X-Total-Count.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] string? region,
        [FromQuery] string? category, [FromQuery] string? search, [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice, [FromQuery] int? beekeeperId, [FromQuery] bool inStock = false,
        [FromQuery] string? sort = null, [FromQuery] int? page = null, [FromQuery] int pageSize = 12)
    {
        var (items, total) = await _products.QueryAsync(new ProductQuery(type, region, category, search, minPrice, maxPrice,
            beekeeperId, inStock, sort, page, pageSize));
        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(items);
    }

    // GET: api/products/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) => Ok(await _products.GetByIdAsync(id));

    // GET: api/products/batch/STR-2026-08 – проверка на партида (произход, пчелин, сертификати)
    [HttpGet("batch/{batchNumber}")]
    public async Task<IActionResult> GetByBatch(string batchNumber)
    {
        var list = await _products.GetByBatchAsync(batchNumber);
        return list.Count == 0 ? NotFound(new { title = "Не е намерена партида с този номер." }) : Ok(list);
    }
}
