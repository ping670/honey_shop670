using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Channels;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

public record EmailMessage(string To, string Subject, string HtmlBody);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

/// <summary>Изпраща писма през SMTP (настройки в „Email“: Host, Port, User, Password, From, EnableSsl).</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    public SmtpEmailSender(IConfiguration config) => _config = config;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var s = _config.GetSection("Email");
        using var client = new SmtpClient(s["Host"], s.GetValue("Port", 587))
        {
            EnableSsl = s.GetValue("EnableSsl", true),
            Timeout = 15000
        };
        if (!string.IsNullOrWhiteSpace(s["User"]))
            client.Credentials = new NetworkCredential(s["User"], s["Password"]);

        var from = s["From"] ?? s["User"] ?? "no-reply@pcheli.local";
        using var mail = new MailMessage { From = new MailAddress(from, s["FromName"] ?? "Пчелари България"), Subject = message.Subject, IsBodyHtml = true, Body = message.HtmlBody };
        mail.SubjectEncoding = mail.BodyEncoding = Encoding.UTF8;
        mail.To.Add(message.To);
        await client.SendMailAsync(mail, ct);
    }
}

/// <summary>Когато SMTP не е конфигуриран, писмата само се записват в лога (удобно за разработка).</summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;
    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation("[ИМЕЙЛ – SMTP не е конфигуриран] До: {To} | Тема: {Subject}", message.To, message.Subject);
        return Task.CompletedTask;
    }
}

/// <summary>Опашка за изходящи писма – заявките не чакат SMTP сървъра.</summary>
public class EmailQueue
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(5000) { FullMode = BoundedChannelFullMode.DropOldest });

    public void Enqueue(EmailMessage message) => _channel.Writer.TryWrite(message);
    public ChannelReader<EmailMessage> Reader => _channel.Reader;
}

public class EmailBackgroundService : BackgroundService
{
    private readonly EmailQueue _queue;
    private readonly IEmailSender _sender;
    private readonly ILogger<EmailBackgroundService> _logger;

    public EmailBackgroundService(EmailQueue queue, IEmailSender sender, ILogger<EmailBackgroundService> logger)
    { _queue = queue; _sender = sender; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try { await _sender.SendAsync(message, stoppingToken); break; }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Неуспешно изпращане на писмо до {To} (опит {Attempt}/3)", message.To, attempt);
                    if (attempt < 3) await Task.Delay(TimeSpan.FromSeconds(5 * attempt), stoppingToken);
                }
            }
        }
    }
}

public interface INotificationService
{
    void OrderCreated(Order order);
    void OrderStatusChanged(Order order);
    void OrderPaid(Order order);
    void ApplicationDecided(BeekeeperApplication application, bool approved);
    void PasswordReset(string email, string name, string link);
    void EmailConfirmation(string email, string name, string link);
    void ContactReceived(ContactMessage message, string? shopEmail, string? beekeeperEmail);
    void BeekeeperNewOrder(string email, string apiaryName, Order order, IReadOnlyCollection<OrderItem> items);
    void StockAvailable(string email, string productName, string url);
    void Newsletter(string email, string subject, string htmlBody, string unsubscribeUrl);
}

public class NotificationService : INotificationService
{
    private readonly EmailQueue _queue;
    private readonly string _baseUrl;
    public NotificationService(EmailQueue queue, IConfiguration config)
    {
        _queue = queue;
        _baseUrl = (config["App:PublicUrl"] ?? string.Empty).TrimEnd('/');
    }

    private static string H(string? s) => HtmlText.Encode(s);

    private string Wrap(string title, string inner) => $"""
        <div style="font-family:Segoe UI,Arial,sans-serif;max-width:560px;margin:auto;color:#2b1a0e">
          <div style="background:#f59e0b;padding:16px 20px;border-radius:10px 10px 0 0;color:#1a0d08;font-weight:700;font-size:18px">🐝 Пчелари България</div>
          <div style="border:1px solid #eadfce;border-top:0;padding:20px;border-radius:0 0 10px 10px">
            <h2 style="margin-top:0">{H(title)}</h2>{inner}
            <p style="color:#8a7a68;font-size:12px;margin-top:24px">Това е автоматично съобщение. Ако имате въпроси, свържете се с нас на телефона от сайта.</p>
          </div>
        </div>
        """;

    private string TrackLink(Order o) => string.IsNullOrEmpty(_baseUrl) ? string.Empty
        : $"<p><a href=\"{H(_baseUrl)}/#track\">Проследете поръчката си</a> с номер <b>{H(o.OrderNumber)}</b> и телефона, който сте въвели.</p>";

    public void OrderCreated(Order o)
    {
        if (string.IsNullOrWhiteSpace(o.CustomerEmail)) return;
        var rows = string.Concat(o.Items.Select(i =>
            $"<tr><td style='padding:4px 8px'>{i.Quantity}× {H(i.ProductName)} <small>({H(i.Weight)})</small></td><td style='padding:4px 8px;text-align:right'>{i.TotalPrice:0.00} €</td></tr>"));
        var pay = o.PaymentStatus == "Чака плащане" ? "<p><b>Очаква се онлайн плащане.</b> Поръчката се обработва след успешно плащане.</p>" : $"<p>Начин на плащане: {H(o.PaymentMethod)}</p>";
        var body = $"""
            <p>Здравейте, {H(o.CustomerName)}! Благодарим за поръчката.</p>
            <p>Номер: <b>{H(o.OrderNumber)}</b></p>
            <table style="width:100%;border-collapse:collapse">{rows}
              <tr><td style='padding:4px 8px'>Доставка ({H(o.Courier)}, {H(o.DeliveryType.ToLowerInvariant())})</td><td style='padding:4px 8px;text-align:right'>{o.ShippingAmount:0.00} €</td></tr>
              <tr><td style='padding:4px 8px'><b>Общо</b></td><td style='padding:4px 8px;text-align:right'><b>{o.TotalAmount:0.00} €</b></td></tr>
            </table>
            {pay}<p>Доставка до: {H(o.DeliveryCity)}, {H(o.DeliveryAddress)}</p>{TrackLink(o)}
            """;
        _queue.Enqueue(new EmailMessage(o.CustomerEmail, $"Поръчка {o.OrderNumber} е приета", Wrap("Поръчката е приета", body)));
    }

    public void OrderStatusChanged(Order o)
    {
        if (string.IsNullOrWhiteSpace(o.CustomerEmail)) return;
        var body = $"<p>Здравейте, {H(o.CustomerName)}!</p><p>Статусът на поръчка <b>{H(o.OrderNumber)}</b> е променен на: <b>{H(o.Status)}</b>.</p>{TrackLink(o)}";
        _queue.Enqueue(new EmailMessage(o.CustomerEmail, $"Поръчка {o.OrderNumber}: {o.Status}", Wrap("Промяна на статуса", body)));
    }

    public void OrderPaid(Order o)
    {
        if (string.IsNullOrWhiteSpace(o.CustomerEmail)) return;
        var body = $"<p>Получихме плащането за поръчка <b>{H(o.OrderNumber)}</b> на стойност <b>{o.TotalAmount:0.00} €</b>. Благодарим Ви!</p>{TrackLink(o)}";
        _queue.Enqueue(new EmailMessage(o.CustomerEmail, $"Плащане за поръчка {o.OrderNumber}", Wrap("Плащането е получено", body)));
    }

    public void ApplicationDecided(BeekeeperApplication a, bool approved)
    {
        if (string.IsNullOrWhiteSpace(a.Email)) return;
        var body = approved
            ? $"<p>Здравейте, {H(a.FullName)}!</p><p>Заявката на пчелин <b>{H(a.ApiaryName)}</b> е <b>одобрена</b>. Продуктите Ви вече са в каталога." +
              (string.IsNullOrEmpty(a.UserId) ? "" : " Влезте в акаунта си, за да управлявате пчелина от личния си панел.") + "</p>"
            : $"<p>Здравейте, {H(a.FullName)}!</p><p>За съжаление заявката на пчелин <b>{H(a.ApiaryName)}</b> не беше одобрена. Свържете се с нас за повече информация.</p>";
        _queue.Enqueue(new EmailMessage(a.Email, approved ? "Заявката Ви е одобрена" : "Относно Вашата заявка", Wrap(approved ? "Добре дошли в платформата!" : "Решение по заявката", body)));
    }

    public void PasswordReset(string email, string name, string link)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        var body = $"<p>Здравейте, {H(name)}!</p><p>Получихме заявка за нова парола. Натиснете бутона, за да зададете такава (връзката е валидна 24 часа):</p>" +
                   $"<p><a href=\"{H(link)}\" style=\"background:#f59e0b;color:#1a0d08;padding:10px 18px;border-radius:8px;text-decoration:none;font-weight:700\">Нова парола</a></p>" +
                   "<p style=\"color:#8a7a68;font-size:13px\">Ако не сте направили заявката Вие, просто игнорирайте това писмо – паролата Ви не е променена.</p>";
        _queue.Enqueue(new EmailMessage(email, "Нова парола за Пчелари България", Wrap("Забравена парола", body)));
    }

    public void EmailConfirmation(string email, string name, string link)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        var body = $"<p>Здравейте, {H(name)}!</p><p>Потвърдете имейла си, за да получавате известия за поръчките:</p>" +
                   $"<p><a href=\"{H(link)}\" style=\"background:#f59e0b;color:#1a0d08;padding:10px 18px;border-radius:8px;text-decoration:none;font-weight:700\">Потвърди имейла</a></p>";
        _queue.Enqueue(new EmailMessage(email, "Потвърдете имейла си", Wrap("Потвърждение на имейл", body)));
    }

    public void ContactReceived(ContactMessage m, string? shopEmail, string? beekeeperEmail)
    {
        var body = $"<p><b>От:</b> {H(m.Name)} &lt;{H(m.Email)}&gt;{(string.IsNullOrWhiteSpace(m.Phone) ? "" : $" · тел. {H(m.Phone)}")}</p>" +
                   $"<p><b>Тема:</b> {H(m.Subject)}</p><p style=\"white-space:pre-wrap\">{H(m.Message)}</p>" +
                   "<p style=\"color:#8a7a68;font-size:13px\">Можете да отговорите директно на този подател.</p>";
        foreach (var to in new[] { shopEmail, beekeeperEmail }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
            _queue.Enqueue(new EmailMessage(to!, $"Съобщение от сайта: {m.Subject}", Wrap("Ново съобщение", body)));
    }

    public void BeekeeperNewOrder(string email, string apiaryName, Order o, IReadOnlyCollection<OrderItem> items)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        var rows = string.Concat(items.Select(i => $"<li>{i.Quantity}× {H(i.ProductName)} <small>({H(i.Weight)})</small></li>"));
        var cod = o.PaymentStatus != "Платена" && o.PaymentMethod.Contains("Наложен", StringComparison.OrdinalIgnoreCase);
        var body = $"<p>Здравейте! Има нова поръчка за пчелин <b>{H(apiaryName)}</b>.</p><p>Номер: <b>{H(o.OrderNumber)}</b></p><ul>{rows}</ul>" +
                   $"<p>Клиент: {H(o.CustomerName)}, тел. {H(o.CustomerPhone)}<br>Доставка: {H(o.Courier)} ({H(o.DeliveryType.ToLowerInvariant())}) до {H(o.DeliveryCity)}, {H(o.DeliveryAddress)}</p>" +
                   (cod ? "<p>Плащането е <b>наложен платеж</b>.</p>" : o.PaymentStatus == "Чака плащане" ? "<p>Очаква се онлайн плащане – изчакайте потвърждение преди изпращане.</p>" : "<p>Поръчката е платена.</p>") +
                   (string.IsNullOrEmpty(_baseUrl) ? "" : $"<p><a href=\"{H(_baseUrl)}/panel.html\">Към панела на пчеларя</a></p>");
        _queue.Enqueue(new EmailMessage(email, $"Нова поръчка {o.OrderNumber}", Wrap("Нова поръчка", body)));
    }

    public void StockAvailable(string email, string productName, string url)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        var body = $"<p>Добра новина! <b>{H(productName)}</b> отново е налично.</p><p><a href=\"{H(url)}\">Разгледайте продукта</a> – количествата са ограничени.</p>";
        _queue.Enqueue(new EmailMessage(email, $"{productName} е отново налично", Wrap("Отново в наличност", body)));
    }

    public void Newsletter(string email, string subject, string htmlBody, string unsubscribeUrl)
    {
        var body = htmlBody + $"<p style=\"color:#8a7a68;font-size:12px;margin-top:20px\">Получавате това писмо, защото сте се абонирали за бюлетина. <a href=\"{H(unsubscribeUrl)}\">Отписване</a></p>";
        _queue.Enqueue(new EmailMessage(email, subject, Wrap(subject, body)));
    }
}
