using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

/// <summary>Администраторски ендпойнти (изисква вход с роля Admin).</summary>
[ApiController]
[Authorize(Roles = DbInitializer.AdminRole)]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IBeekeeperService _beekeepers;
    private readonly IProductService _products;
    private readonly IOrderService _orders;
    private readonly IReviewService _reviews;
    private readonly IPromoService _promos;

    public AdminController(IBeekeeperService beekeepers, IProductService products, IOrderService orders,
        IReviewService reviews, IPromoService promos)
    {
        _beekeepers = beekeepers; _products = products; _orders = orders; _reviews = reviews; _promos = promos;
    }

    // ----- Заявки за пчелари -----
    [HttpGet("applications")]
    public async Task<IActionResult> Applications() => Ok(await _beekeepers.GetApplicationsAsync());

    [HttpPost("applications/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id) => Ok(await _beekeepers.ApproveAsync(id));

    [HttpPost("applications/{id:int}/reject")]
    public async Task<IActionResult> Reject(int id) { await _beekeepers.RejectAsync(id); return NoContent(); }

    // ----- Продукти -----
    [HttpGet("products")]
    public async Task<IActionResult> Products() => Ok(await _products.GetAllAsync(null, null, null, null));

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] ProductUpsertDto dto)
    {
        var created = await _products.CreateAsync(dto);
        return CreatedAtAction(nameof(Products), new { id = created.Id }, created);
    }

    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpsertDto dto) => Ok(await _products.UpdateAsync(id, dto));

    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id) { await _products.DeleteAsync(id); return NoContent(); }

    // ----- Поръчки -----
    [HttpGet("orders")]
    public async Task<IActionResult> Orders() => Ok(await _orders.GetRecentAsync());

    [HttpPut("orders/{id:int}/status")]
    public async Task<IActionResult> OrderStatus(int id, [FromBody] OrderStatusDto dto) => Ok(await _orders.UpdateStatusAsync(id, dto.Status));

    // ----- Отзиви -----
    [HttpGet("reviews")]
    public async Task<IActionResult> Reviews() => Ok(await _reviews.GetAllAsync());

    [HttpPost("reviews/{id:int}/approve")]
    public async Task<IActionResult> ApproveReview(int id) => Ok(await _reviews.SetApprovedAsync(id, true));

    [HttpPost("reviews/{id:int}/hide")]
    public async Task<IActionResult> HideReview(int id) => Ok(await _reviews.SetApprovedAsync(id, false));

    [HttpDelete("reviews/{id:int}")]
    public async Task<IActionResult> DeleteReview(int id) { await _reviews.DeleteAsync(id); return NoContent(); }

    // ----- Промо кодове -----
    [HttpGet("promocodes")]
    public async Task<IActionResult> Promos() => Ok(await _promos.GetAllAsync());

    [HttpPost("promocodes")]
    public async Task<IActionResult> CreatePromo([FromBody] PromoCodeUpsertDto dto) => Ok(await _promos.CreateAsync(dto));

    [HttpPut("promocodes/{id:int}")]
    public async Task<IActionResult> UpdatePromo(int id, [FromBody] PromoCodeUpsertDto dto) => Ok(await _promos.UpdateAsync(id, dto));

    [HttpDelete("promocodes/{id:int}")]
    public async Task<IActionResult> DeletePromo(int id) { await _promos.DeleteAsync(id); return NoContent(); }
}
