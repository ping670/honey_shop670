using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // POST: api/orders
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] Order order)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Генериране на номер на поръчка: BG-ГГММДД-XXXX
        order.OrderNumber = $"BG-{DateTime.UtcNow:yyMMdd}-{new Random().Next(1000, 9999)}";
        order.CreatedAt = DateTime.UtcNow;
        order.Status = "Приета";

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            orderNumber = order.OrderNumber,
            totalAmount = order.TotalAmount,
            message = "Поръчката Ви е приета успешно! Пчеларите бяха уведомени и ще се свържат с Вас за доставка."
        });
    }

    // GET: api/orders
    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Take(50)
            .ToListAsync();

        return Ok(orders);
    }

    // GET: api/orders/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetPlatformStats()
    {
        var beekeepersCount = await _context.Beekeepers.CountAsync();
        var productsCount = await _context.Products.CountAsync();
        var honeyCount = await _context.Products.CountAsync(p => p.Type == "Мед");
        var pollenCount = await _context.Products.CountAsync(p => p.Type == "Пчелен прашец");
        var ordersCount = await _context.Orders.CountAsync();
        var regionsCount = await _context.Beekeepers.Select(b => b.Region).Distinct().CountAsync();
        var totalHives = await _context.Beekeepers.SumAsync(b => b.HiveCount);

        return Ok(new
        {
            beekeepersCount,
            productsCount,
            honeyCount,
            pollenCount,
            ordersCount,
            regionsCount,
            totalHives
        });
    }
}
