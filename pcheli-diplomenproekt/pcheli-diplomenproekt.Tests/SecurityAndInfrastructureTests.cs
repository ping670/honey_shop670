using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using pcheli_diplomenproekt.Models;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Tests;

/// <summary>Stripe webhook, проверка „не съм робот“, качване на снимки, адреси и журнал на действията.</summary>
public class SecurityAndInfrastructureTests
{
    // ---------- Stripe ----------
    private const string Secret = "whsec_test_secret";

    private static string Sign(string payload, long timestamp, string secret = Secret) =>
        $"t={timestamp},v1={Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"))).ToLowerInvariant()}";

    [Fact]
    public void StripeSignature_AcceptsValid_RejectsWrongSecretTamperedAndStale()
    {
        var now = DateTimeOffset.UtcNow;
        var ts = now.ToUnixTimeSeconds();
        const string body = "{\"id\":\"evt_1\"}";

        Assert.True(StripePaymentService.VerifySignature(body, Sign(body, ts), Secret, now));
        Assert.False(StripePaymentService.VerifySignature(body, Sign(body, ts, "other"), Secret, now));
        Assert.False(StripePaymentService.VerifySignature(body + " ", Sign(body, ts), Secret, now));          // променено тяло
        Assert.False(StripePaymentService.VerifySignature(body, Sign(body, ts - 3600), Secret, now));          // стар подпис (replay)
        Assert.False(StripePaymentService.VerifySignature(body, null, Secret, now));
        Assert.False(StripePaymentService.VerifySignature(body, "t=abc,v1=zz", Secret, now));
    }

    private static StripePaymentService Stripe(TestDb db, FakeNotifier notifier, bool configured = true)
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(configured
            ? new Dictionary<string, string?> { ["Stripe:SecretKey"] = "sk_test_x", ["Stripe:WebhookSecret"] = Secret }
            : new Dictionary<string, string?>()).Build();
        return new StripePaymentService(db.Context, null!, notifier, cfg, NullLogger<StripePaymentService>.Instance);
    }

    private static string CompletedEvent(string orderNumber, long cents, string status = "paid", string currency = "eur", string type = "checkout.session.completed") =>
        $"{{\"type\":\"{type}\",\"data\":{{\"object\":{{\"client_reference_id\":\"{orderNumber}\",\"payment_status\":\"{status}\",\"amount_total\":{cents},\"currency\":\"{currency}\"}}}}}}";

    private static async Task<(TestDb Db, string Number, decimal Total)> PendingCardOrderAsync()
    {
        var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var dto = Make.Order(product.Variants.First(v => v.Price == 20.00m).Id);
        var order = await Make.Orders(db).CreateAsync(dto);
        var o = db.Context.Orders.Single();
        o.PaymentStatus = StripePaymentService.Pending; db.Context.SaveChanges();
        return (db, o.OrderNumber, o.TotalAmount);
    }

    [Fact]
    public async Task Webhook_MarksOrderPaid_AddsEventAndNotifies_OnlyOnce()
    {
        var (db, number, total) = await PendingCardOrderAsync();
        using var _ = db;
        var notifier = new FakeNotifier();
        var stripe = Stripe(db, notifier);
        var payload = CompletedEvent(number, StripePaymentService.ToCents(total));
        var sig = Sign(payload, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        Assert.True(await stripe.HandleWebhookAsync(payload, sig));
        Assert.True(await stripe.HandleWebhookAsync(payload, sig));   // Stripe може да повтаря събитията

        var o = db.Context.Orders.Include(x => x.Events).AsNoTracking().Single();
        Assert.Equal(StripePaymentService.Paid, o.PaymentStatus);
        Assert.Single(o.Events, e => e.Status == "Платена");
        Assert.Single(notifier.Paid);
    }

    [Fact]
    public async Task Webhook_RejectsBadSignature_AndWrongAmountOrCurrency()
    {
        var (db, number, total) = await PendingCardOrderAsync();
        using var _ = db;
        var stripe = Stripe(db, new FakeNotifier());
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var cents = StripePaymentService.ToCents(total);

        var good = CompletedEvent(number, cents);
        Assert.False(await stripe.HandleWebhookAsync(good, Sign(good, ts, "wrong-secret")));
        Assert.Equal(StripePaymentService.Pending, db.Context.Orders.AsNoTracking().Single().PaymentStatus);

        var lowAmount = CompletedEvent(number, cents - 100);
        Assert.True(await stripe.HandleWebhookAsync(lowAmount, Sign(lowAmount, ts)));
        var usd = CompletedEvent(number, cents, currency: "usd");
        Assert.True(await stripe.HandleWebhookAsync(usd, Sign(usd, ts)));
        var unpaid = CompletedEvent(number, cents, status: "unpaid");
        Assert.True(await stripe.HandleWebhookAsync(unpaid, Sign(unpaid, ts)));

        Assert.Equal(StripePaymentService.Pending, db.Context.Orders.AsNoTracking().Single().PaymentStatus);   // не е маркирана като платена
    }

    [Fact]
    public async Task Webhook_ExpiredSession_MarksFailed_AndUnknownOrderIsAcknowledged()
    {
        var (db, number, _) = await PendingCardOrderAsync();
        using var __ = db;
        var stripe = Stripe(db, new FakeNotifier());
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var expired = CompletedEvent(number, 0, type: "checkout.session.expired");
        Assert.True(await stripe.HandleWebhookAsync(expired, Sign(expired, ts)));
        Assert.Equal(StripePaymentService.Failed, db.Context.Orders.AsNoTracking().Single().PaymentStatus);

        var unknown = CompletedEvent("BG-000000-0000", 100);
        Assert.True(await stripe.HandleWebhookAsync(unknown, Sign(unknown, ts)));
    }

    [Fact]
    public async Task Stripe_WithoutKeys_IsDisabled_AndRefusesWebhooksAndCheckout()
    {
        using var db = new TestDb();
        var stripe = Stripe(db, new FakeNotifier(), configured: false);
        var payload = "{}";

        Assert.False(stripe.Enabled);
        Assert.False(await stripe.HandleWebhookAsync(payload, Sign(payload, DateTimeOffset.UtcNow.ToUnixTimeSeconds())));
        await Assert.ThrowsAsync<BusinessException>(() => stripe.CreateCheckoutUrlAsync("BG-1", "0888", "http://x"));
    }

    // ---------- Captcha ----------
    private static (CaptchaService Svc, ITimeLimitedDataProtector Reader) NewCaptcha(int minSeconds = 0, bool enabled = true)
    {
        var provider = new EphemeralDataProtectionProvider();
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Captcha:MinSeconds"] = minSeconds.ToString(), ["Captcha:Enabled"] = enabled.ToString() }).Build();
        return (new CaptchaService(provider, new MemoryCache(new MemoryCacheOptions()), cfg),
            provider.CreateProtector("pcheli.captcha.v1").ToTimeLimitedDataProtector());
    }

    private static string AnswerOf(ITimeLimitedDataProtector reader, CaptchaChallenge c) => reader.Unprotect(c.Token).Split('|')[0];

    [Fact]
    public void Captcha_AcceptsCorrectAnswerOnce_RejectsWrongReusedAndForged()
    {
        var (svc, reader) = NewCaptcha();
        var c = svc.Create();
        var answer = AnswerOf(reader, c);

        Assert.False(svc.Verify(c.Token, (int.Parse(answer) + 1).ToString()));   // грешен отговор
        Assert.True(svc.Verify(c.Token, answer));
        Assert.False(svc.Verify(c.Token, answer));                                 // повторна употреба
        Assert.False(svc.Verify("подправен-токен", answer));
        Assert.False(svc.Verify(null, answer));
        Assert.False(svc.Verify(c.Token, null));
        Assert.False(svc.Verify(c.Token + "x", answer));
    }

    [Fact]
    public void Captcha_RejectsAnswersSubmittedTooFast()
    {
        var (svc, reader) = NewCaptcha(minSeconds: 60);
        var c = svc.Create();

        Assert.False(svc.Verify(c.Token, AnswerOf(reader, c)));
    }

    [Fact]
    public void Captcha_WhenDisabled_AllowsEverything_AndQuestionsAreSolvable()
    {
        var (off, _) = NewCaptcha(enabled: false);
        Assert.False(off.Enabled);
        Assert.True(off.Verify(null, null));

        var (svc, reader) = NewCaptcha();
        for (var i = 0; i < 30; i++)
        {
            var c = svc.Create();
            Assert.Contains("Колко е", c.Question);
            Assert.True(int.TryParse(AnswerOf(reader, c), out var n) && n >= 2);
        }
    }

    // ---------- Качване на снимки ----------
    [Fact]
    public void Upload_DetectsImageTypeByContent_NotByName()
    {
        Assert.Equal(".jpg", UploadService.DetectExtension(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 }));
        Assert.Equal(".png", UploadService.DetectExtension(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 }));
        Assert.Equal(".webp", UploadService.DetectExtension(Encoding.ASCII.GetBytes("RIFF....WEBP")));
        Assert.Null(UploadService.DetectExtension(Encoding.ASCII.GetBytes("<script>alert(1)</script>")));
        Assert.Null(UploadService.DetectExtension(Encoding.ASCII.GetBytes("MZ\u0090\u0000\u0003\u0000\u0000\u0000\u0004\u0000\u0000\u0000")));   // exe
        Assert.Null(UploadService.DetectExtension(Array.Empty<byte>()));
    }

    // ---------- Адреси ----------
    [Theory]
    [InlineData("", true)]
    [InlineData("./images/acacia.jpg", true)]
    [InlineData("/uploads/abc.jpg", true)]
    [InlineData("images/x.png", true)]
    [InlineData("https://example.com/lab.pdf", true)]
    [InlineData("http://example.com/x.png", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html;base64,AAAA", false)]
    [InlineData("//evil.example/x.png", false)]
    [InlineData("/uploads/../secret", false)]
    [InlineData("ftp://x/y", false)]
    public void UrlRules_AllowsOnlySafeAddresses(string url, bool safe) => Assert.Equal(safe, UrlRules.IsSafe(url));

    // ---------- Журнал ----------
    [Fact]
    public async Task Audit_StoresEntries_FiltersAndPaginates()
    {
        using var db = new TestDb();
        var svc = new AuditService(db.Context);
        for (var i = 0; i < 7; i++)
            await svc.LogAsync(new AuditLog { UserName = i % 2 == 0 ? "admin" : "pchelar", Method = "PUT", Action = "Admin/UpdateProduct", Path = "/api/admin/products/" + i, EntityId = i.ToString(), StatusCode = 200, Details = "Продукт „Мед“" });

        var page = await svc.GetAsync(1, 3, null, null);
        var admin = await svc.GetAsync(1, 50, "admin", null);
        var byEntity = await svc.GetAsync(1, 50, null, "5");

        Assert.Equal(7, page.Total);
        Assert.Equal(3, page.Items.Count);
        Assert.Equal(4, admin.Total);
        Assert.Contains(byEntity.Items, a => a.EntityId == "5");
        Assert.True(page.Items[0].At >= page.Items[1].At);            // най-новите първи
    }
}
