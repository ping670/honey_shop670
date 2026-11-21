using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

/// <summary>Публична конфигурация за интерфейса: дали е включено плащането с карта, проверката „не съм робот“, куриери и цени.</summary>
[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromServices] IPaymentService payments, [FromServices] ICaptchaService captcha,
        [FromServices] IShippingService shipping, [FromServices] IConfiguration config) =>
        Ok(new PublicConfigDto(payments.Enabled, captcha.Enabled, shipping.FreeShippingFrom,
            SubscriptionService.DiscountPercent, shipping.Couriers.ToList(), ShopInfo.From(config)));
}

/// <summary>Форма „Контакти“ – към магазина или към конкретен пчелар.</summary>
[ApiController]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly IContactService _contact;
    public ContactController(IContactService contact) => _contact = contact;

    [HttpPost]
    [EnableRateLimiting("forms")]
    [RequireCaptcha]
    public async Task<IActionResult> Send([FromBody] ContactDto dto)
    {
        await _contact.SubmitAsync(dto);
        return Ok(new { success = true, message = "Благодарим! Съобщението е изпратено – ще Ви отговорим възможно най-скоро." });
    }
}

/// <summary>Записване и отписване от бюлетина.</summary>
[ApiController]
[Route("api/newsletter")]
public class NewsletterController : ControllerBase
{
    private readonly INewsletterService _newsletter;
    public NewsletterController(INewsletterService newsletter) => _newsletter = newsletter;

    [HttpPost("subscribe")]
    [EnableRateLimiting("forms")]
    [RequireCaptcha]
    public async Task<IActionResult> Subscribe([FromBody] NewsletterSubscribeDto dto)
    {
        await _newsletter.SubscribeAsync(dto.Email);
        return Ok(new { success = true, message = "Благодарим! Записахте се за бюлетина." });
    }

    // GET: връзката от писмото – отписва с един клик и показва проста страница
    [HttpGet("unsubscribe")]
    [EnableRateLimiting("track")]
    public async Task<IActionResult> Unsubscribe([FromQuery] string token)
    {
        var ok = await _newsletter.UnsubscribeAsync(token);
        var text = ok ? "Отписахте се от бюлетина. Съжаляваме, че си тръгвате!" : "Връзката за отписване не е валидна.";
        return Content($"<!doctype html><html lang=\"bg\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><meta name=\"robots\" content=\"noindex\"><title>Отписване</title></head>" +
                       $"<body style=\"font-family:system-ui,sans-serif;max-width:520px;margin:12vh auto;padding:0 16px;text-align:center\"><h1>🐝</h1><p>{text}</p><p><a href=\"/\">Към сайта</a></p></body></html>",
            "text/html; charset=utf-8");
    }
}

/// <summary>„Уведоми ме, когато е наличен“.</summary>
[ApiController]
[Route("api/stock-alerts")]
public class StockAlertsController : ControllerBase
{
    private readonly IStockAlertService _alerts;
    public StockAlertsController(IStockAlertService alerts) => _alerts = alerts;

    [HttpPost]
    [EnableRateLimiting("forms")]
    public async Task<IActionResult> Subscribe([FromBody] StockAlertDto dto)
    {
        await _alerts.SubscribeAsync(dto.ProductId, dto.Email);
        return Ok(new { success = true, message = "Ще Ви уведомим по имейл, когато продуктът е отново наличен." });
    }
}

[ApiController]
[Route("api/captcha")]
public class CaptchaController : ControllerBase
{
    private readonly ICaptchaService _captcha;
    public CaptchaController(ICaptchaService captcha) => _captcha = captcha;

    [HttpGet]
    [EnableRateLimiting("api")]
    public IActionResult Get()
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(_captcha.Create());
    }
}

public class CheckoutSessionRequest
{
    [Required, MaxLength(40)] public string OrderNumber { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Phone { get; set; } = string.Empty;
}

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly IConfiguration _config;
    public PaymentsController(IPaymentService payments, IConfiguration config) { _payments = payments; _config = config; }

    // POST: api/payments/checkout – връща адрес на Stripe Checkout за поръчка, чакаща плащане
    [HttpPost("checkout")]
    [EnableRateLimiting("track")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutSessionRequest dto)
    {
        var baseUrl = (_config["App:PublicUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        return Ok(new PaymentSessionDto(await _payments.CreateCheckoutUrlAsync(dto.OrderNumber.Trim().TrimStart('#'), dto.Phone, baseUrl)));
    }

    // POST: api/payments/webhook – извиква се от Stripe; подписът се проверява
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var ok = await _payments.HandleWebhookAsync(payload, Request.Headers["Stripe-Signature"].FirstOrDefault());
        return ok ? Ok() : BadRequest();
    }
}

[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articles;
    public ArticlesController(IArticleService articles) => _articles = articles;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 9, [FromQuery] string? category = null)
    {
        var (items, total) = await _articles.GetPublishedAsync(page, pageSize, category);
        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(items);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug) => Ok(await _articles.GetBySlugAsync(slug));
}

[ApiController]
[Authorize(Roles = DbInitializer.AdminRole + "," + DbInitializer.BeekeeperRole)]
[Route("api/uploads")]
public class UploadsController : ControllerBase
{
    private readonly IUploadService _uploads;
    public UploadsController(IUploadService uploads) => _uploads = uploads;

    // POST: api/uploads (multipart/form-data, поле „file“) – връща адрес на качената снимка
    [HttpPost]
    [EnableRateLimiting("api")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file) => Ok(new { url = await _uploads.SaveImageAsync(file) });
}
