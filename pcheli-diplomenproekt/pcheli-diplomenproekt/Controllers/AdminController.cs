using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

/// <summary>Администраторски ендпойнти (изисква вход с роля Admin). Всяка промяна се записва в журнала на действията.</summary>
[ApiController]
[Authorize(Roles = DbInitializer.AdminRole)]
[ServiceFilter(typeof(AuditFilter))]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IBeekeeperService _beekeepers;
    private readonly IProductService _products;
    private readonly IOrderService _orders;
    private readonly IReviewService _reviews;
    private readonly IPromoService _promos;
    private readonly IStatsService _stats;
    private readonly IArticleService _articles;
    private readonly IAuditService _audit;
    private readonly ISubscriptionService _subscriptions;
    private readonly IConfiguration _config;
    private readonly IRegionService _regions;
    private readonly IUserAdminService _userAdmin;
    private readonly IAccountSecurityService _security;
    private readonly IContactService _contact;
    private readonly INewsletterService _newsletter;

    public AdminController(IBeekeeperService beekeepers, IProductService products, IOrderService orders,
        IReviewService reviews, IPromoService promos, IStatsService stats, IArticleService articles, IAuditService audit,
        ISubscriptionService subscriptions, IConfiguration config, IRegionService regions, IUserAdminService userAdmin,
        IAccountSecurityService security, IContactService contact, INewsletterService newsletter)
    {
        _beekeepers = beekeepers; _products = products; _orders = orders; _reviews = reviews; _promos = promos;
        _stats = stats; _articles = articles; _audit = audit; _subscriptions = subscriptions; _config = config;
        _regions = regions; _userAdmin = userAdmin; _security = security; _contact = contact; _newsletter = newsletter;
    }

    private string BaseUrl => (_config["App:PublicUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');

    // ----- Табло -----
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() => Ok(await _stats.GetDashboardAsync());

    // ----- Заявки за пчелари -----
    [HttpGet("applications")]
    public async Task<IActionResult> Applications() => Ok(await _beekeepers.GetApplicationsAsync());

    [HttpPost("applications/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id) => Ok(await _beekeepers.ApproveAsync(id));

    [HttpPost("applications/{id:int}/reject")]
    public async Task<IActionResult> Reject(int id) { await _beekeepers.RejectAsync(id); return NoContent(); }

    /// <summary>Свързва пчелин с вече регистриран потребител (по имейл), който получава роля Beekeeper и достъп до панела.</summary>
    [HttpPost("beekeepers/{id:int}/link-account")]
    public async Task<IActionResult> LinkBeekeeperAccount(int id, [FromBody] LinkBeekeeperAccountDto dto)
    {
        this.AuditDetails($"Акаунт: {dto.Email}");
        return Ok(await _beekeepers.LinkAccountAsync(id, dto.Email));
    }

    // ----- Пчелари (пълно управление) -----
    [HttpGet("beekeepers")]
    public async Task<IActionResult> Beekeepers() => Ok(await _beekeepers.GetAllForAdminAsync());

    [HttpPut("beekeepers/{id:int}")]
    public async Task<IActionResult> UpdateBeekeeper(int id, [FromBody] BeekeeperAdminUpsertDto dto)
    {
        this.AuditDetails($"Пчелин „{dto.ApiaryName}“");
        return Ok(await _beekeepers.AdminUpdateAsync(id, dto));
    }

    [HttpPost("beekeepers/{id:int}/hide")]
    public async Task<IActionResult> HideBeekeeper(int id) => Ok(await _beekeepers.SetActiveAsync(id, false));

    [HttpPost("beekeepers/{id:int}/show")]
    public async Task<IActionResult> ShowBeekeeper(int id) => Ok(await _beekeepers.SetActiveAsync(id, true));

    [HttpDelete("beekeepers/{id:int}")]
    public async Task<IActionResult> DeleteBeekeeper(int id) { await _beekeepers.DeleteAsync(id); return NoContent(); }

    // ----- Региони -----
    [HttpGet("regions")]
    public async Task<IActionResult> Regions() => Ok(await _regions.GetAllAsync());

    [HttpPost("regions")]
    public async Task<IActionResult> CreateRegion([FromBody] RegionUpsertDto dto)
    {
        this.AuditDetails($"Регион „{dto.Name}“");
        return Ok(await _regions.CreateAsync(dto));
    }

    [HttpPut("regions/{id:int}")]
    public async Task<IActionResult> UpdateRegion(int id, [FromBody] RegionUpsertDto dto)
    {
        this.AuditDetails($"Регион „{dto.Name}“");
        return Ok(await _regions.UpdateAsync(id, dto));
    }

    [HttpDelete("regions/{id:int}")]
    public async Task<IActionResult> DeleteRegion(int id) { await _regions.DeleteAsync(id); return NoContent(); }

    // ----- Потребители -----
    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        var r = await _userAdmin.ListAsync(search, page, pageSize);
        Response.Headers["X-Total-Count"] = r.Total.ToString();
        return Ok(r.Items);
    }

    [HttpPost("users/{id}/lock")]
    public async Task<IActionResult> LockUser(string id)
    {
        this.AuditDetails("Блокиране на акаунт");
        return Ok(await _userAdmin.SetLockedAsync(id, true, User.FindFirstValue(ClaimTypes.NameIdentifier)));
    }

    [HttpPost("users/{id}/unlock")]
    public async Task<IActionResult> UnlockUser(string id)
    {
        this.AuditDetails("Отблокиране на акаунт");
        return Ok(await _userAdmin.SetLockedAsync(id, false, User.FindFirstValue(ClaimTypes.NameIdentifier)));
    }

    /// <summary>Изпраща на потребителя писмо с връзка за нова парола (администраторът не вижда и не задава паролата).</summary>
    [HttpPost("users/{id}/send-reset")]
    public async Task<IActionResult> SendReset(string id, [FromServices] Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> users)
    {
        var u = await users.FindByIdAsync(id) ?? throw new BusinessException("Потребителят не е намерен.", StatusCodes.Status404NotFound);
        if (string.IsNullOrWhiteSpace(u.Email)) throw new BusinessException("Акаунтът няма имейл.");
        this.AuditDetails("Писмо за нова парола");
        await _security.RequestPasswordResetAsync(u.Email, BaseUrl);
        return NoContent();
    }

    // ----- Съобщения от формата „Контакти“ -----
    [HttpGet("messages")]
    public async Task<IActionResult> Messages() => Ok(await _contact.GetAllAsync());

    [HttpPost("messages/{id:int}/handled")]
    public async Task<IActionResult> MessageHandled(int id, [FromQuery] bool value = true) => Ok(await _contact.SetHandledAsync(id, value));

    [HttpDelete("messages/{id:int}")]
    public async Task<IActionResult> DeleteMessage(int id) { await _contact.DeleteAsync(id); return NoContent(); }

    // ----- Бюлетин -----
    [HttpGet("newsletter")]
    public async Task<IActionResult> Subscribers() => Ok(await _newsletter.GetAllAsync());

    [HttpPost("newsletter/send")]
    public async Task<IActionResult> SendNewsletter([FromBody] NewsletterSendDto dto)
    {
        var n = await _newsletter.SendAsync(dto, BaseUrl);
        this.AuditDetails($"Бюлетин „{dto.Subject}“ до {n} абонати");
        return Ok(new { sent = n });
    }

    // ----- Продукти -----
    [HttpGet("products")]
    public async Task<IActionResult> Products() => Ok((await _products.QueryAsync(new ProductQuery(IncludeHidden: true))).Items);

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] ProductUpsertDto dto)
    {
        var created = await _products.CreateAsync(dto);
        this.AuditDetails($"Продукт „{created.Name}“");
        return CreatedAtAction(nameof(Products), new { id = created.Id }, created);
    }

    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpsertDto dto)
    {
        this.AuditDetails($"Продукт „{dto.Name}“");
        return Ok(await _products.UpdateAsync(id, dto));
    }

    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id) { await _products.DeleteAsync(id); return NoContent(); }

    // ----- Наличности -----
    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory([FromQuery] bool lowOnly = false) => Ok(await _products.GetInventoryAsync(null, lowOnly));

    [HttpPut("inventory/{variantId:int}")]
    public async Task<IActionResult> SetStock(int variantId, [FromBody] StockUpdateDto dto)
    {
        this.AuditDetails($"Наличност: {dto.StockQuantity}");
        return Ok(await _products.SetStockAsync(variantId, dto.StockQuantity, null));
    }

    // ----- Поръчки -----
    // GET: api/admin/orders?page=1&pageSize=25&status=Приета&search=...&from=2026-01-01&to=2026-12-31 (общият брой е в X-Total-Count)
    [HttpGet("orders")]
    public async Task<IActionResult> Orders([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? status = null,
        [FromQuery] string? search = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (items, total) = await _orders.GetPageAsync(new OrderFilter(page, pageSize, status, search, from, to));
        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(items);
    }

    [HttpPut("orders/{id:int}/status")]
    public async Task<IActionResult> OrderStatus(int id, [FromBody] OrderStatusDto dto)
    {
        this.AuditDetails($"Нов статус: {dto.Status}");
        return Ok(await _orders.UpdateStatusAsync(id, dto.Status));
    }

    // GET: api/admin/orders/export?status=&from=&to= – CSV за Excel
    [HttpGet("orders/export")]
    public async Task<IActionResult> ExportOrders([FromQuery] string? status = null, [FromQuery] string? search = null,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (items, total) = await _orders.GetPageAsync(new OrderFilter(1, 5000, status, search, from, to));
        this.AuditDetails($"Експорт на {items.Count} от {total} поръчки");
        return File(OrderExport.ToCsv(items), "text/csv; charset=utf-8", $"porachki-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }

    // GET: api/admin/orders/labels?ids=1,2,3 – страница за печат на товарителници
    [HttpGet("orders/labels")]
    public async Task<IActionResult> Labels([FromQuery] string ids)
    {
        var list = (ids ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : 0).Where(n => n > 0).ToList();
        if (list.Count == 0) throw new BusinessException("Изберете поне една поръчка.");
        var orders = await _orders.GetByIdsAsync(list);
        this.AuditDetails($"Печат на {orders.Count} товарителници");
        var html = OrderExport.LabelsHtml(orders, _config["Shop:SenderName"] ?? "Пчелари България", _config["Shop:SenderPhone"] ?? "0888 123 456");
        return Content(html, "text/html; charset=utf-8");
    }

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

    // ----- Блог -----
    [HttpGet("articles")]
    public async Task<IActionResult> Articles() => Ok(await _articles.GetAllAsync());

    [HttpPost("articles")]
    public async Task<IActionResult> CreateArticle([FromBody] ArticleUpsertDto dto)
    {
        var a = await _articles.CreateAsync(dto);
        this.AuditDetails($"Статия „{a.Title}“");
        return Ok(a);
    }

    [HttpPut("articles/{id:int}")]
    public async Task<IActionResult> UpdateArticle(int id, [FromBody] ArticleUpsertDto dto)
    {
        this.AuditDetails($"Статия „{dto.Title}“");
        return Ok(await _articles.UpdateAsync(id, dto));
    }

    [HttpDelete("articles/{id:int}")]
    public async Task<IActionResult> DeleteArticle(int id) { await _articles.DeleteAsync(id); return NoContent(); }

    // ----- Абонаменти (само за преглед) -----
    [HttpGet("subscriptions")]
    public async Task<IActionResult> Subscriptions() => Ok(await _subscriptions.GetAllAsync());

    // ----- Журнал на действията -----
    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? user = null, [FromQuery] string? search = null) =>
        Ok(await _audit.GetAsync(page, pageSize, user, search));
}
