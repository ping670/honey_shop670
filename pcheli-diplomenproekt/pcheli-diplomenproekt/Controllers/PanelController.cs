using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

/// <summary>
/// Личен панел на пчелара. Всички операции са ограничени до пчелина, свързан с акаунта – пчеларят не може да вижда
/// и променя чужди продукти или поръчки. Промените се записват в журнала на действията.
/// </summary>
[ApiController]
[Authorize(Roles = DbInitializer.BeekeeperRole)]
[ServiceFilter(typeof(AuditFilter))]
[Route("api/panel")]
public class PanelController : ControllerBase
{
    private readonly IBeekeeperService _beekeepers;
    private readonly IProductService _products;
    private readonly IOrderService _orders;

    public PanelController(IBeekeeperService beekeepers, IProductService products, IOrderService orders)
    { _beekeepers = beekeepers; _products = products; _orders = orders; }

    private async Task<int> MyIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var id = userId is null ? null : await _beekeepers.GetIdByUserAsync(userId);
        return id ?? throw new BusinessException("Акаунтът ви не е свързан с пчелин. Свържете се с администратор.", StatusCodes.Status403Forbidden);
    }

    private async Task EnsureOwnProductAsync(int productId, int myId)
    {
        var owner = await _products.GetBeekeeperIdAsync(productId);
        // непознат и чужд продукт дават един и същ отговор
        if (owner != myId) throw new BusinessException("Продуктът не е намерен.", StatusCodes.Status404NotFound);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me() => Ok(await _beekeepers.GetDetailAsync(await MyIdAsync(), includeHidden: true));

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] BeekeeperProfileUpdateDto dto) =>
        Ok(await _beekeepers.UpdateProfileAsync(await MyIdAsync(), dto));

    [HttpGet("products")]
    public async Task<IActionResult> Products()
    {
        var id = await MyIdAsync();
        return Ok((await _products.QueryAsync(new ProductQuery(BeekeeperId: id, IncludeHidden: true))).Items);
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] ProductUpsertDto dto)
    {
        dto.BeekeeperId = await MyIdAsync();   // винаги собственият пчелин, каквото и да е изпратено
        var created = await _products.CreateAsync(dto);
        this.AuditDetails($"Продукт „{created.Name}“");
        return Ok(created);
    }

    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpsertDto dto)
    {
        var myId = await MyIdAsync();
        await EnsureOwnProductAsync(id, myId);
        dto.BeekeeperId = myId;
        this.AuditDetails($"Продукт „{dto.Name}“");
        return Ok(await _products.UpdateAsync(id, dto));
    }

    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        await EnsureOwnProductAsync(id, await MyIdAsync());
        await _products.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory([FromQuery] bool lowOnly = false) =>
        Ok(await _products.GetInventoryAsync(await MyIdAsync(), lowOnly));

    [HttpPut("inventory/{variantId:int}")]
    public async Task<IActionResult> SetStock(int variantId, [FromBody] StockUpdateDto dto)
    {
        this.AuditDetails($"Наличност: {dto.StockQuantity}");
        return Ok(await _products.SetStockAsync(variantId, dto.StockQuantity, await MyIdAsync()));
    }

    [HttpGet("orders")]
    public async Task<IActionResult> Orders() => Ok(await _orders.GetForBeekeeperAsync(await MyIdAsync()));

    // PUT: api/panel/orders/5/status – пчеларят придвижва собствена поръчка напред
    [HttpPut("orders/{id:int}/status")]
    public async Task<IActionResult> OrderStatus(int id, [FromBody] OrderStatusDto dto)
    {
        this.AuditDetails($"Нов статус: {dto.Status}");
        return Ok(await _orders.UpdateStatusForBeekeeperAsync(await MyIdAsync(), id, dto.Status));
    }
}
