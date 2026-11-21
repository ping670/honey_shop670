using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

/// <summary>
/// Страници за продукти (/p/{id}) и пчелари (/b/{id}), генерирани на сървъра: заглавие, описание, Open Graph и
/// структурирани данни schema.org (Product с оферта и оценка, LocalBusiness), за да се индексират и споделят добре.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class SeoController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    public SeoController(ApplicationDbContext db, IConfiguration config) { _db = db; _config = config; }

    private string BaseUrl => (_config["App:PublicUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
    private string Abs(string path) => path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : BaseUrl + "/" + path.TrimStart('.', '/');
    private static string Local(string path) => path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : "/" + path.TrimStart('.', '/');
    private static string LdJson(object o) => JsonSerializer.Serialize(o).Replace("<", "\\u003c");

    [HttpGet("/p/{id:int}")]
    public async Task<IActionResult> Product(int id)
    {
        var p = await _db.Products.AsNoTracking().Include(x => x.Variants).Include(x => x.Beekeeper).ThenInclude(b => b!.Region)
            .FirstOrDefaultAsync(x => x.Id == id && (x.Beekeeper == null || x.Beekeeper.IsActive));
        if (p is null) return NotFound();

        var reviews = await _db.Reviews.AsNoTracking().Where(r => r.ProductId == id && r.IsApproved).Select(r => r.Rating).ToListAsync();
        var variants = p.Variants.OrderBy(v => v.Id).ToList();
        var available = p.InStock && variants.Any(v => v.InStock && v.StockQuantity > 0);
        var enc = HtmlText.Encoder;
        var url = $"{BaseUrl}/p/{p.Id}";
        var minPrice = variants.Count > 0 ? variants.Min(v => v.Price) : 0m;
        var desc = string.IsNullOrWhiteSpace(p.Description)
            ? $"{p.Name} – {p.Origin}. Директно от пчелар, с доставка в цяла България."
            : (p.Description.Length > 200 ? p.Description[..197] + "…" : p.Description);

        var sb = new StringBuilder();
        sb.Append($"<a class=\"back\" href=\"/#products\">← Към каталога</a><article class=\"article\"><span class=\"tag\">{enc.Encode(p.Type)}</span><h1>{enc.Encode(p.Name)}</h1>");
        sb.Append($"<img class=\"cover\" src=\"{enc.Encode(Local(p.Image))}\" alt=\"{enc.Encode(p.Name)}\">");
        if (!string.IsNullOrWhiteSpace(p.Description)) sb.Append($"<p>{enc.Encode(p.Description)}</p>");
        sb.Append("<ul>");
        sb.Append($"<li><b>Произход:</b> {enc.Encode(p.Origin)}</li>");
        if (p.Beekeeper != null) sb.Append($"<li><b>Пчелин:</b> <a href=\"/b/{p.Beekeeper.Id}\">{enc.Encode(p.Beekeeper.ApiaryName)}</a> ({enc.Encode(p.Beekeeper.FullName)})</li>");
        if (!string.IsNullOrWhiteSpace(p.HarvestDate)) sb.Append($"<li><b>Реколта:</b> {enc.Encode(p.HarvestDate)}</li>");
        if (!string.IsNullOrWhiteSpace(p.BatchNumber)) sb.Append($"<li><b>Партида:</b> {enc.Encode(p.BatchNumber)}</li>");
        if (!string.IsNullOrWhiteSpace(p.Purity)) sb.Append($"<li><b>Качество:</b> {enc.Encode(p.Purity)}</li>");
        if (!string.IsNullOrWhiteSpace(p.TastingNotes)) sb.Append($"<li><b>Вкус:</b> {enc.Encode(p.TastingNotes)}</li>");
        sb.Append("</ul><h2>Опаковки и цени</h2><ul>");
        foreach (var v in variants)
            sb.Append($"<li>{enc.Encode(v.Label)} – <b>{v.Price:0.00} €</b>{(v.InStock && v.StockQuantity > 0 ? "" : " <small>(изчерпано)</small>")}</li>");
        sb.Append("</ul>");

        if (available)
            sb.Append($"<div class=\"cta\"><a class=\"btn\" href=\"/#products\" onclick=\"try{{sessionStorage.setItem('pb_open_product','{p.Id}')}}catch(e){{}}\">🛒 Поръчай от каталога</a></div>");
        else
            sb.Append($"""
                <h2>Изчерпано в момента</h2>
                <form id="alert-form" style="display:flex;gap:8px;flex-wrap:wrap;max-width:460px">
                  <input id="alert-email" type="email" required maxlength="100" placeholder="Вашият имейл" style="flex:1;min-width:200px;padding:10px;border-radius:8px;border:1px solid #888">
                  <button class="btn" type="submit">🔔 Уведоми ме</button>
                </form><p id="alert-msg" role="status"></p>
                """);
        sb.Append("</article>");

        var ld = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org", ["@type"] = "Product", ["name"] = p.Name, ["description"] = desc,
            ["image"] = Abs(p.Image), ["sku"] = $"P{p.Id}", ["url"] = url,
            ["offers"] = new Dictionary<string, object?>
            {
                ["@type"] = "AggregateOffer", ["priceCurrency"] = "EUR", ["lowPrice"] = minPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                ["highPrice"] = (variants.Count > 0 ? variants.Max(v => v.Price) : 0m).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                ["offerCount"] = variants.Count,
                ["availability"] = available ? "https://schema.org/InStock" : "https://schema.org/OutOfStock"
            }
        };
        if (p.Beekeeper != null) ld["brand"] = new Dictionary<string, string> { ["@type"] = "Brand", ["name"] = p.Beekeeper.ApiaryName };
        if (reviews.Count > 0)   // само реални одобрени отзиви – без измислени оценки
            ld["aggregateRating"] = new Dictionary<string, object>
            {
                ["@type"] = "AggregateRating", ["ratingValue"] = Math.Round(reviews.Average(), 1).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                ["reviewCount"] = reviews.Count
            };

        var script = $"var f=document.getElementById('alert-form');if(f)f.addEventListener('submit',function(e){{e.preventDefault();var m=document.getElementById('alert-msg');" +
                     $"fetch('/api/stock-alerts',{{method:'POST',headers:{{'Content-Type':'application/json'}},body:JSON.stringify({{productId:{p.Id},email:document.getElementById('alert-email').value}})}})" +
                     ".then(function(r){return r.json().catch(function(){return{}}).then(function(d){m.textContent=r.ok?(d.message||'Готово.'):(d.message||d.detail||d.title||'Грешка.');});})" +
                     ".catch(function(){m.textContent='Няма връзка със сървъра.';});});";

        return Content(BlogPage.Layout($"{p.Name} – Пчелари България", desc, sb.ToString(), url, Abs(p.Image), LdJson(ld), "product", available ? null : script),
            "text/html; charset=utf-8");
    }

    [HttpGet("/b/{id:int}")]
    public async Task<IActionResult> Beekeeper(int id)
    {
        var b = await _db.Beekeepers.AsNoTracking().Include(x => x.Region).Include(x => x.Products).ThenInclude(p => p.Variants)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (b is null) return NotFound();

        var enc = HtmlText.Encoder;
        var url = $"{BaseUrl}/b/{b.Id}";
        var desc = string.IsNullOrWhiteSpace(b.Bio) ? $"Пчелин „{b.ApiaryName}“ – {b.Region?.Name}. Мед и пчелни продукти директно от пчеларя." : (b.Bio.Length > 200 ? b.Bio[..197] + "…" : b.Bio);
        var sb = new StringBuilder();
        sb.Append($"<a class=\"back\" href=\"/#beekeepers\">← Всички пчелари</a><article class=\"article\"><h1>{enc.Encode(b.ApiaryName)}</h1>");
        sb.Append($"<p class=\"meta\">{enc.Encode(b.FullName)} · {enc.Encode(b.Region?.Name ?? "")} · {enc.Encode(b.CityOrVillage)} · регистрация {enc.Encode(b.RegistrationNumber)}</p>");
        if (!string.IsNullOrWhiteSpace(b.PhotoUrl)) sb.Append($"<img class=\"cover\" src=\"{enc.Encode(Local(b.PhotoUrl))}\" alt=\"{enc.Encode(b.ApiaryName)}\">");
        if (!string.IsNullOrWhiteSpace(b.Bio)) sb.Append($"<p>{enc.Encode(b.Bio)}</p>");
        if (!string.IsNullOrWhiteSpace(b.Story)) sb.Append($"<p>{enc.Encode(b.Story)}</p>");
        sb.Append($"<p>🐝 {b.HiveCount} кошера · опит {b.ExperienceYears} г.</p><h2>Продукти</h2><ul>");
        foreach (var p in b.Products.OrderBy(p => p.Id))
            sb.Append($"<li><a href=\"/p/{p.Id}\">{enc.Encode(p.Name)}</a>{(p.Variants.Count > 0 ? $" – от {p.Variants.Min(v => v.Price):0.00} €" : "")}</li>");
        sb.Append($"</ul><div class=\"cta\"><a class=\"btn\" href=\"/beekeeper.html?id={b.Id}\">Към пълния профил и поръчка</a> <a class=\"btn\" href=\"/contact.html?beekeeper={b.Id}\">✉️ Пишете на пчеларя</a></div></article>");

        var ld = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org", ["@type"] = "LocalBusiness", ["name"] = b.ApiaryName, ["description"] = desc, ["url"] = url,
            ["address"] = new Dictionary<string, string>
            {
                ["@type"] = "PostalAddress", ["addressLocality"] = b.CityOrVillage, ["addressRegion"] = b.Region?.Name ?? "", ["addressCountry"] = "BG"
            }
        };
        if (!string.IsNullOrWhiteSpace(b.PhotoUrl)) ld["image"] = Abs(b.PhotoUrl);

        return Content(BlogPage.Layout($"{b.ApiaryName} – пчелин в {b.Region?.Name} | Пчелари България", desc, sb.ToString(), url,
            string.IsNullOrWhiteSpace(b.PhotoUrl) ? null : Abs(b.PhotoUrl), LdJson(ld), "profile"), "text/html; charset=utf-8");
    }
}

