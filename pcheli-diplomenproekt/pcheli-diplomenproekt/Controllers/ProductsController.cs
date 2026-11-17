using Microsoft.AspNetCore.Mvc;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    public ProductsController(IProductService products) => _products = products;

    // GET: api/products?type=Мед&region=Странджа&category=manov&search=...
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] string? region,
        [FromQuery] string? category, [FromQuery] string? search)
        => Ok(await _products.GetAllAsync(type, region, category, search));

    // GET: api/products/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) => Ok(await _products.GetByIdAsync(id));
}
