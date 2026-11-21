using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly IConfiguration _config;
    public OrdersController(IOrderService orders, IConfiguration config) { _orders = orders; _config = config; }

    // POST: api/orders/cancel – гост или клиент отказва поръчка (номер + телефон), докато е „Приета“
    [HttpPost("cancel")]
    [EnableRateLimiting("track")]
    public async Task<IActionResult> Cancel([FromBody] CancelOrderDto dto) =>
        Ok(await _orders.CancelByCustomerAsync(dto.OrderNumber, dto.Phone, User.FindFirstValue(ClaimTypes.NameIdentifier), dto.Reason));

    // GET: api/orders/receipt?number=…&phone=… – разписка за печат
    [HttpGet("receipt")]
    [EnableRateLimiting("track")]
    public async Task<IActionResult> Receipt([FromQuery] string number, [FromQuery] string phone) =>
        Content(OrderExport.ReceiptHtml(await _orders.FindForCustomerAsync(number, phone, User.FindFirstValue(ClaimTypes.NameIdentifier)),
            ShopInfo.From(_config)), "text/html; charset=utf-8");

    // POST: api/orders – цените, отстъпката и доставката се изчисляват на сървъра
    [HttpPost]
    [EnableRateLimiting("orders")]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto) =>
        Ok(await _orders.CreateAsync(dto, new OrderContext(User.FindFirstValue(ClaimTypes.NameIdentifier))));

    // POST: api/orders/quote – оценка на сумата (куриер, тип доставка, промо код) без създаване на поръчка
    [HttpPost("quote")]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> Quote([FromBody] QuoteRequestDto dto) => Ok(await _orders.QuoteAsync(dto));

    // GET: api/orders/track?number=BG-261118-1234&phone=0888123456
    [HttpGet("track")]
    [EnableRateLimiting("track")]
    public async Task<IActionResult> Track([FromQuery] string number, [FromQuery] string phone) =>
        Ok(await _orders.TrackAsync(number, phone));

    // GET: api/orders/stats
    [HttpGet("stats")]
    public async Task<IActionResult> Stats() => Ok(await _orders.GetStatsAsync());
}
