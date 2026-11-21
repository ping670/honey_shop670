using System.Globalization;
using System.Text;
using pcheli_diplomenproekt.Dtos;

namespace pcheli_diplomenproekt.Services;

/// <summary>Експорт на поръчки към CSV (отваря се директно в Excel) и печат на товарителници.</summary>
public static class OrderExport
{
    private static readonly string[] Header =
    {
        "Номер", "Дата (UTC)", "Статус", "Плащане", "Статус на плащане", "Клиент", "Телефон", "Имейл", "Град",
        "Адрес", "Куриер", "Тип доставка", "Артикули", "Междинна сума", "Отстъпка", "Доставка", "Общо", "Промо код",
        "Подарък", "Съобщение към подаръка", "Бележки"
    };

    /// <summary>Клетки, започващи с = + - @, се префиксират с „'“, за да не се изпълнят като формула в Excel.</summary>
    public static string Cell(string? value)
    {
        var v = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
        if (v.Length > 0 && "=+-@\t".Contains(v[0])) v = "'" + v;
        return v.Contains(';') || v.Contains('"') ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
    }

    private static string Money(decimal d) => d.ToString("0.00", CultureInfo.GetCultureInfo("bg-BG"));

    /// <summary>CSV с разделител „;“ (стандартен за Excel с български настройки). Файлът започва с BOM за правилна кирилица.</summary>
    public static byte[] ToCsv(IEnumerable<OrderDto> orders)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', Header));
        foreach (var o in orders)
        {
            var items = string.Join(" | ", o.Items.Select(i => $"{i.Quantity}× {i.ProductName} ({i.Weight})"));
            sb.AppendLine(string.Join(';', new[]
            {
                Cell(o.OrderNumber), o.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), Cell(o.Status),
                Cell(o.PaymentMethod), Cell(o.PaymentStatus), Cell(o.CustomerName), Cell(o.CustomerPhone), Cell(o.CustomerEmail),
                Cell(o.DeliveryCity), Cell(o.DeliveryAddress), Cell(o.Courier), Cell(o.DeliveryType), Cell(items),
                Money(o.Subtotal), Money(o.DiscountAmount), Money(o.ShippingAmount), Money(o.TotalAmount), Cell(o.PromoCode),
                o.IsGift ? "да" : "не", Cell(o.GiftMessage), Cell(o.Notes)
            }));
        }
        return new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    /// <summary>Разписка за поръчка (за печат или „Запази като PDF“). Не замества данъчна фактура – тя се издава от търговеца по искане на клиента.</summary>
    public static string ReceiptHtml(OrderDto o, ShopInfoDto shop)
    {
        var e = HtmlText.Encoder;
        var sb = new StringBuilder();
        sb.Append($$"""
            <!doctype html><html lang="bg"><head><meta charset="utf-8"><title>Разписка {{e.Encode(o.OrderNumber)}}</title>
            <style>
              body { font-family: Arial, sans-serif; color: #111; max-width: 760px; margin: 24px auto; padding: 0 16px }
              h1 { font-size: 22px; margin: 0 0 4px } .muted { color: #555; font-size: 13px }
              .row { display: flex; justify-content: space-between; gap: 20px; flex-wrap: wrap; margin: 14px 0 }
              table { width: 100%; border-collapse: collapse; margin-top: 10px; font-size: 14px }
              th, td { text-align: left; padding: 7px 8px; border-bottom: 1px solid #ddd } th { background: #f5f5f5 }
              td.n, th.n { text-align: right } tfoot td { border-bottom: 0; font-weight: 700 }
              .noprint { padding: 10px; background: #fffbe6; border: 1px solid #e5d88a; font-size: 13px; margin-bottom: 14px }
              @media print { .noprint { display: none } }
            </style></head><body>
            <div class="noprint">Натиснете <b>Ctrl+P</b>, за да отпечатате или запазите като PDF.</div>
            """);
        sb.Append($"<h1>Разписка за поръчка {e.Encode(o.OrderNumber)}</h1><div class=\"muted\">Дата: {o.CreatedAt:dd.MM.yyyy HH:mm} (UTC) · Статус: {e.Encode(o.Status)}</div>");
        sb.Append("<div class=\"row\"><div><b>Търговец</b><br>");
        sb.Append(e.Encode(shop.CompanyName));
        if (shop.Eik.Length > 0) sb.Append($"<br>ЕИК: {e.Encode(shop.Eik)}");
        if (shop.Address.Length > 0) sb.Append($"<br>{e.Encode(shop.Address)}");
        if (shop.Email.Length > 0) sb.Append($"<br>{e.Encode(shop.Email)}");
        if (shop.Phone.Length > 0) sb.Append($"<br>тел. {e.Encode(shop.Phone)}");
        if (shop.VatNote.Length > 0) sb.Append($"<br><span class=\"muted\">{e.Encode(shop.VatNote)}</span>");
        sb.Append($"</div><div><b>Клиент</b><br>{e.Encode(o.CustomerName)}<br>тел. {e.Encode(o.CustomerPhone)}");
        if (o.CustomerEmail.Length > 0) sb.Append($"<br>{e.Encode(o.CustomerEmail)}");
        sb.Append($"<br>{e.Encode(o.DeliveryCity)}, {e.Encode(o.DeliveryAddress)}</div></div>");
        sb.Append("<table><thead><tr><th>Артикул</th><th class=\"n\">Кол.</th><th class=\"n\">Ед. цена</th><th class=\"n\">Общо</th></tr></thead><tbody>");
        foreach (var i in o.Items)
            sb.Append($"<tr><td>{e.Encode(i.ProductName)} <span class=\"muted\">({e.Encode(i.Weight)})</span></td><td class=\"n\">{i.Quantity}</td><td class=\"n\">{Money(i.UnitPrice)} €</td><td class=\"n\">{Money(i.TotalPrice)} €</td></tr>");
        sb.Append("</tbody><tfoot>");
        sb.Append($"<tr><td colspan=\"3\" class=\"n\" style=\"font-weight:400\">Междинна сума</td><td class=\"n\" style=\"font-weight:400\">{Money(o.Subtotal)} €</td></tr>");
        if (o.DiscountAmount > 0)
            sb.Append($"<tr><td colspan=\"3\" class=\"n\" style=\"font-weight:400\">Отстъпка{(string.IsNullOrEmpty(o.PromoCode) ? "" : " (" + e.Encode(o.PromoCode) + ")")}</td><td class=\"n\" style=\"font-weight:400\">−{Money(o.DiscountAmount)} €</td></tr>");
        sb.Append($"<tr><td colspan=\"3\" class=\"n\" style=\"font-weight:400\">Доставка ({e.Encode(o.Courier)})</td><td class=\"n\" style=\"font-weight:400\">{Money(o.ShippingAmount)} €</td></tr>");
        sb.Append($"<tr><td colspan=\"3\" class=\"n\">Общо</td><td class=\"n\">{Money(o.TotalAmount)} €</td></tr></tfoot></table>");
        sb.Append($"<p class=\"muted\">Плащане: {e.Encode(o.PaymentMethod)} · {e.Encode(o.PaymentStatus)}</p></body></html>");
        return sb.ToString();
    }

    /// <summary>Страница за печат: по една товарителница (A6) на поръчка. Наложеният платеж е с отбелязана сума за събиране.</summary>
    public static string LabelsHtml(IReadOnlyList<OrderDto> orders, string senderName, string senderPhone)
    {
        var e = HtmlText.Encoder;
        var sb = new StringBuilder();
        sb.Append("""
            <!doctype html><html lang="bg"><head><meta charset="utf-8"><title>Товарителници</title>
            <style>
              @page { size: A6; margin: 6mm }
              body { font-family: Arial, sans-serif; margin: 0; color: #000 }
              .label { page-break-after: always; border: 2px solid #000; padding: 10px 12px; height: 128mm; box-sizing: border-box; display: flex; flex-direction: column; gap: 6px }
              .label:last-child { page-break-after: auto }
              h1 { font-size: 15px; margin: 0; display: flex; justify-content: space-between }
              .box { border: 1px solid #000; padding: 6px 8px; font-size: 13px }
              .box b { display: block; font-size: 10px; text-transform: uppercase; letter-spacing: .5px; margin-bottom: 2px }
              .big { font-size: 17px; font-weight: 700 }
              .cod { font-size: 20px; font-weight: 700; text-align: center; border: 3px double #000; padding: 6px }
              ul { margin: 2px 0 0 16px; padding: 0; font-size: 12px }
              .noprint { padding: 10px; background: #fffbe6; border-bottom: 1px solid #e5d88a; font-size: 13px }
              @media print { .noprint { display: none } }
            </style></head><body>
            <div class="noprint">Натиснете <b>Ctrl+P</b>, за да отпечатате. Размер на хартията: A6.</div>
            """);
        foreach (var o in orders)
        {
            var cod = o.PaymentStatus != "Платена" && o.PaymentMethod.Contains("Наложен", StringComparison.OrdinalIgnoreCase);
            var apiaries = string.Join(", ", o.Items.Select(i => i.ApiaryName).Where(a => a.Length > 0).Distinct());
            sb.Append("<div class=\"label\">");
            sb.Append($"<h1><span>{e.Encode(o.OrderNumber)}</span><span>{e.Encode(o.Courier)} · {e.Encode(o.DeliveryType)}</span></h1>");
            sb.Append($"<div class=\"box\"><b>Получател</b><span class=\"big\">{e.Encode(o.CustomerName)}</span><br>тел. {e.Encode(o.CustomerPhone)}<br>{e.Encode(o.DeliveryCity)}, {e.Encode(o.DeliveryAddress)}</div>");
            sb.Append($"<div class=\"box\"><b>Подател</b>{e.Encode(senderName)}{(apiaries.Length > 0 ? " · " + e.Encode(apiaries) : "")}<br>тел. {e.Encode(senderPhone)}</div>");
            sb.Append("<div class=\"box\"><b>Съдържание</b><ul>");
            foreach (var i in o.Items) sb.Append($"<li>{i.Quantity}× {e.Encode(i.ProductName)} ({e.Encode(i.Weight)})</li>");
            sb.Append("</ul></div>");
            if (o.IsGift) sb.Append($"<div class=\"box\"><b>🎁 Подарък</b>{e.Encode(o.GiftMessage ?? "")}</div>");
            if (!string.IsNullOrWhiteSpace(o.Notes)) sb.Append($"<div class=\"box\"><b>Бележка</b>{e.Encode(o.Notes)}</div>");
            sb.Append(cod ? $"<div class=\"cod\">НАЛОЖЕН ПЛАТЕЖ: {Money(o.TotalAmount)} €</div>"
                          : "<div class=\"cod\">ПЛАТЕНО — без наложен платеж</div>");
            sb.Append("</div>");
        }
        sb.Append("</body></html>");
        return sb.ToString();
    }
}
