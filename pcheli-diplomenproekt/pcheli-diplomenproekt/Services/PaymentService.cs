using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

public interface IPaymentService
{
    bool Enabled { get; }
    Task<string> CreateCheckoutUrlAsync(string orderNumber, string phone, string baseUrl);
    Task<bool> HandleWebhookAsync(string payload, string? signatureHeader);
}

/// <summary>
/// Онлайн плащане с карта през Stripe Checkout (hosted страница – данните на картата не минават през нашия сървър).
/// Включва се с Stripe:SecretKey и Stripe:WebhookSecret (User Secrets / променливи на средата). Без ключове
/// опцията „карта“ е изключена и остава наложеният платеж. Сумите са в евро.
/// </summary>
public class StripePaymentService : IPaymentService
{
    public const string Pending = "Чака плащане";
    public const string Paid = "Платена";
    public const string Failed = "Неуспешно плащане";

    private static readonly TimeSpan SignatureTolerance = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _http;
    private readonly INotificationService _notify;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly string? _secretKey;
    private readonly string? _webhookSecret;

    public StripePaymentService(ApplicationDbContext db, IHttpClientFactory http, INotificationService notify,
        IConfiguration config, ILogger<StripePaymentService> logger)
    {
        _db = db; _http = http; _notify = notify; _logger = logger;
        _secretKey = config["Stripe:SecretKey"];
        _webhookSecret = config["Stripe:WebhookSecret"];
    }

    public bool Enabled => !string.IsNullOrWhiteSpace(_secretKey) && !string.IsNullOrWhiteSpace(_webhookSecret);

    public static long ToCents(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    public async Task<string> CreateCheckoutUrlAsync(string orderNumber, string phone, string baseUrl)
    {
        if (!Enabled) throw new BusinessException("Плащането с карта в момента не е налично.", StatusCodes.Status503ServiceUnavailable);

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        if (order is null || !OrderService.PhoneMatches(order.CustomerPhone, phone))
            throw new BusinessException("Поръчката не е намерена.", StatusCodes.Status404NotFound);
        if (order.PaymentStatus == Paid) throw new BusinessException("Поръчката вече е платена.", StatusCodes.Status409Conflict);
        if ((order.PaymentStatus != Pending && order.PaymentStatus != Failed) || order.Status == "Отказана")
            throw new BusinessException("Тази поръчка не очаква онлайн плащане.", StatusCodes.Status409Conflict);

        var form = new Dictionary<string, string>
        {
            ["mode"] = "payment",
            ["success_url"] = $"{baseUrl}/?paid={Uri.EscapeDataString(order.OrderNumber)}",
            ["cancel_url"] = $"{baseUrl}/?payment=cancelled&order={Uri.EscapeDataString(order.OrderNumber)}",
            ["client_reference_id"] = order.OrderNumber,
            ["metadata[order_number]"] = order.OrderNumber,
            ["line_items[0][quantity]"] = "1",
            ["line_items[0][price_data][currency]"] = "eur",
            ["line_items[0][price_data][unit_amount]"] = ToCents(order.TotalAmount).ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price_data][product_data][name]"] = $"Поръчка {order.OrderNumber} – Пчелари България"
        };
        if (!string.IsNullOrWhiteSpace(order.CustomerEmail)) form["customer_email"] = order.CustomerEmail;

        var client = _http.CreateClient("stripe");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions")
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _secretKey);

        using var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Stripe отказа създаване на сесия ({Status}): {Body}", (int)response.StatusCode, json);
            throw new BusinessException("Плащането не можа да бъде стартирано. Опитайте отново или изберете наложен платеж.", StatusCodes.Status502BadGateway);
        }

        using var doc = JsonDocument.Parse(json);
        var url = doc.RootElement.GetProperty("url").GetString();
        order.StripeSessionId = doc.RootElement.GetProperty("id").GetString();
        order.PaymentStatus = Pending;   // нов опит след неуспешно или изтекло плащане
        await _db.SaveChangesAsync();
        return url ?? throw new BusinessException("Плащането не можа да бъде стартирано.", StatusCodes.Status502BadGateway);
    }

    /// <summary>Проверява подписа „Stripe-Signature“ (HMAC-SHA256 върху „timestamp.payload“).</summary>
    public static bool VerifySignature(string payload, string? header, string secret, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(header)) return false;
        long timestamp = 0;
        var signatures = new List<string>();
        foreach (var part in header.Split(','))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            if (kv[0].Trim() == "t") long.TryParse(kv[1], out timestamp);
            else if (kv[0].Trim() == "v1") signatures.Add(kv[1].Trim());
        }
        if (timestamp == 0 || signatures.Count == 0) return false;
        if ((now - DateTimeOffset.FromUnixTimeSeconds(timestamp)).Duration() > SignatureTolerance) return false;

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
        return signatures.Any(s =>
        {
            try { return CryptographicOperations.FixedTimeEquals(expected, Convert.FromHexString(s)); }
            catch (FormatException) { return false; }
        });
    }

    public async Task<bool> HandleWebhookAsync(string payload, string? signatureHeader)
    {
        if (!Enabled || !VerifySignature(payload, signatureHeader, _webhookSecret!, DateTimeOffset.UtcNow))
            return false;

        using var doc = JsonDocument.Parse(payload);
        var type = doc.RootElement.GetProperty("type").GetString();
        var obj = doc.RootElement.GetProperty("data").GetProperty("object");
        var orderNumber = obj.TryGetProperty("client_reference_id", out var r) ? r.GetString() : null;
        if (string.IsNullOrEmpty(orderNumber)) return true; // не е наше събитие – потвърждаваме, за да не се повтаря

        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        if (order is null) { _logger.LogWarning("Stripe webhook за непозната поръчка {Order}", orderNumber); return true; }

        switch (type)
        {
            case "checkout.session.completed":
            case "checkout.session.async_payment_succeeded":
                var paidStatus = obj.TryGetProperty("payment_status", out var ps) ? ps.GetString() : null;
                var amount = obj.TryGetProperty("amount_total", out var at) && at.ValueKind == JsonValueKind.Number ? at.GetInt64() : -1;
                var currency = obj.TryGetProperty("currency", out var cur) ? cur.GetString() : null;
                if (paidStatus != "paid") break; // async метод – чакаме async_payment_succeeded
                if (amount != ToCents(order.TotalAmount) || !string.Equals(currency, "eur", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Stripe: несъответствие на сумата за {Order} (получено {Amount} {Cur}, очаквано {Expected})",
                        orderNumber, amount, currency, ToCents(order.TotalAmount));
                    break;
                }
                if (order.PaymentStatus != Paid)
                {
                    order.PaymentStatus = Paid;
                    order.Events.Add(new OrderEvent { Status = "Платена", Note = "Плащането с карта е получено", At = DateTime.UtcNow });
                    await _db.SaveChangesAsync();
                    _notify.OrderPaid(order);
                }
                break;

            case "checkout.session.expired":
            case "checkout.session.async_payment_failed":
                if (order.PaymentStatus == Pending)
                {
                    order.PaymentStatus = Failed;
                    order.Events.Add(new OrderEvent { Status = Failed, At = DateTime.UtcNow });
                    await _db.SaveChangesAsync();
                }
                break;
        }
        return true;
    }
}
