using Microsoft.AspNetCore.Mvc;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    public OrdersController(IOrderService orders) => _orders = orders;

    // POST: api/orders – цените, отстъпката и доставката се изчисляват на сървъра
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto) => Ok(await _orders.CreateAsync(dto));

    // GET: api/orders/stats
    [HttpGet("stats")]
    public async Task<IActionResult> Stats() => Ok(await _orders.GetStatsAsync());
}
