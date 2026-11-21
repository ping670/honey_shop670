using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

/// <summary>Блогът се генерира на сървъра (чист HTML със заглавия, описание и структурирани данни), за да се индексира добре от търсачките.</summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class BlogController : Controller
{
    private readonly IArticleService _articles;
    private readonly IConfiguration _config;
    public BlogController(IArticleService articles, IConfiguration config) { _articles = articles; _config = config; }

    private string BaseUrl => (_config["App:PublicUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');

    [HttpGet("/blog")]
    public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] string? category = null)
    {
        const int size = 9;
        var (items, total) = await _articles.GetPublishedAsync(page, size, category);
        var enc = HtmlText.Encoder;
        var sb = new StringBuilder();
        sb.Append("<h1>Блог на Пчелари България</h1><p class=\"lead\">Съвети за избор и съхранение на мед, ползи от пчелните продукти и лесни рецепти.</p>");
        sb.Append("<div class=\"cat-bar\"><a href=\"/blog\"" + (category is null ? " class=\"on\"" : "") + ">Всички</a>");
        foreach (var c in new[] { "Съвети", "Здраве", "Рецепти" })
            sb.Append($"<a href=\"/blog?category={Uri.EscapeDataString(c)}\"{(c == category ? " class=\"on\"" : "")}>{enc.Encode(c)}</a>");
        sb.Append("</div><div class=\"cards\">");
        foreach (var a in items)
        {
            sb.Append($"""
                <a class="card" href="/blog/{enc.Encode(a.Slug)}">
                  <img src="{enc.Encode(Img(a.CoverImage))}" alt="" loading="lazy">
                  <div class="card-body"><span class="tag">{enc.Encode(a.Category)}</span>
                  <h2>{enc.Encode(a.Title)}</h2><p>{enc.Encode(a.Summary)}</p>
                  <small>{a.PublishedAt:dd.MM.yyyy} · {enc.Encode(a.Author)}</small></div>
                </a>
                """);
        }
        if (items.Count == 0) sb.Append("<p>Все още няма публикувани статии.</p>");
        sb.Append("</div>");

        var pages = (int)Math.Ceiling(total / (double)size);
        if (pages > 1)
        {
            sb.Append("<nav class=\"pager\">");
            for (var p = 1; p <= pages; p++)
                sb.Append($"<a href=\"/blog?page={p}{(category is null ? "" : "&category=" + Uri.EscapeDataString(category))}\"{(p == page ? " class=\"on\"" : "")}>{p}</a>");
            sb.Append("</nav>");
        }

        return Html(BlogPage.Layout("Блог – Пчелари България", "Съвети за избор на мед, ползи от пчелните продукти и лесни рецепти от българските пчелари.",
            sb.ToString(), $"{BaseUrl}/blog", null, null));
    }

    [HttpGet("/blog/{slug}")]
    public async Task<IActionResult> Article(string slug)
    {
        ArticleDto a;
        try { a = await _articles.GetBySlugAsync(slug); }
        catch (BusinessException) { return NotFound(); }

        var enc = HtmlText.Encoder;
        var url = $"{BaseUrl}/blog/{a.Slug}";
        var body = $"""
            <article class="article">
              <a class="back" href="/blog">← Всички статии</a>
              <span class="tag">{enc.Encode(a.Category)}</span>
              <h1>{enc.Encode(a.Title)}</h1>
              <p class="meta">{a.PublishedAt:dd.MM.yyyy} · {enc.Encode(a.Author)}</p>
              <img class="cover" src="{enc.Encode(Img(a.CoverImage))}" alt="{enc.Encode(a.Title)}">
              {ArticleRenderer.ToHtml(a.Content)}
              <div class="cta"><a class="btn" href="/#products">🍯 Разгледайте мед и прашец директно от пчелари</a></div>
            </article>
            """;

        var ld = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Article",
            ["headline"] = a.Title,
            ["description"] = a.Summary,
            ["datePublished"] = a.PublishedAt.ToString("o"),
            ["author"] = new Dictionary<string, string> { ["@type"] = "Organization", ["name"] = a.Author },
            ["image"] = AbsoluteUrl(a.CoverImage),
            ["mainEntityOfPage"] = url
        }).Replace("<", "\\u003c");   // защита срещу „</script>“ в текста

        return Html(BlogPage.Layout($"{a.Title} – Пчелари България", a.Summary, body, url, AbsoluteUrl(a.CoverImage), ld));
    }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap([FromServices] ApplicationDbContext db)
    {
        var enc = System.Security.SecurityElement.Escape;
        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");
        void Add(string loc, DateTime? last = null) =>
            sb.Append($"<url><loc>{enc(loc)}</loc>{(last.HasValue ? $"<lastmod>{last:yyyy-MM-dd}</lastmod>" : "")}</url>\n");

        Add(BaseUrl + "/"); Add(BaseUrl + "/blog");
        foreach (var page in new[] { "contact", "delivery", "returns", "terms", "privacy", "cookies" })
            Add($"{BaseUrl}/{page}.html");
        foreach (var a in await db.Articles.AsNoTracking().Where(a => a.IsPublished).Select(a => new { a.Slug, a.PublishedAt }).ToListAsync())
            Add($"{BaseUrl}/blog/{a.Slug}", a.PublishedAt);
        foreach (var id in await db.Beekeepers.AsNoTracking().Where(b => b.IsActive).Select(b => b.Id).ToListAsync())
            Add($"{BaseUrl}/b/{id}");
        foreach (var id in await db.Products.AsNoTracking().Where(p => p.Beekeeper == null || p.Beekeeper.IsActive).Select(p => p.Id).ToListAsync())
            Add($"{BaseUrl}/p/{id}");
        sb.Append("</urlset>");
        return Content(sb.ToString(), "application/xml; charset=utf-8");
    }

    [HttpGet("/robots.txt")]
    public IActionResult Robots() => Content(
        $"User-agent: *\nAllow: /\nDisallow: /api/\nDisallow: /admin.html\nDisallow: /panel.html\nDisallow: /account.html\n\nSitemap: {BaseUrl}/sitemap.xml\n",
        "text/plain; charset=utf-8");

    private string AbsoluteUrl(string path) =>
        path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : BaseUrl + "/" + path.TrimStart('.', '/');

    private static string Img(string path) => path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : "/" + path.TrimStart('.', '/');

    private ContentResult Html(string html) => Content(html, "text/html; charset=utf-8");
}

internal static class BlogPage
{
    public static string Layout(string title, string description, string body, string canonical, string? ogImage, string? jsonLd,
        string ogType = "article", string? extraScript = null)
    {
        var enc = HtmlText.Encoder;
        return $$"""
            <!doctype html>
            <html lang="bg">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{enc.Encode(title)}}</title>
              <meta name="description" content="{{enc.Encode(description)}}">
              <link rel="canonical" href="{{enc.Encode(canonical)}}">
              <meta property="og:type" content="{{enc.Encode(ogType)}}"><meta property="og:site_name" content="Пчелари България"><meta property="og:locale" content="bg_BG"><meta property="og:title" content="{{enc.Encode(title)}}">
              <meta property="og:description" content="{{enc.Encode(description)}}"><meta property="og:url" content="{{enc.Encode(canonical)}}">
              {{(ogImage is null ? "" : $"<meta property=\"og:image\" content=\"{enc.Encode(ogImage)}\">")}}
              <meta name="theme-color" content="#140a05">
              <link rel="manifest" href="/manifest.webmanifest">
              <link rel="icon" href="/favicon.ico">
              <link rel="stylesheet" href="/pages.css">
              <script>try{var t=localStorage.getItem('pchelari_theme');if(t)document.documentElement.dataset.theme=t;}catch(e){}</script>
              {{(jsonLd is null ? "" : $"<script type=\"application/ld+json\">{jsonLd}</script>")}}
            </head>
            <body class="page">
              <header class="site-header"><div class="wrap">
                <a class="brand" href="/">🐝 Пчелари България</a>
                <nav><a href="/">Начало</a><a href="/#products">Каталог</a><a href="/blog" class="current">Блог</a><a href="/#beekeepers">Пчелари</a><span id="site-tools" data-tools="theme"></span></nav>
              </div></header>
              <main class="wrap">{{body}}</main>
              <footer class="site-footer"><div class="wrap">© 2026 Пчелари България – национална платформа за чист мед и пчелен прашец.
                <div class="legal-links"><a href="/contact.html">Контакти</a> · <a href="/delivery.html">Доставка и плащане</a> · <a href="/returns.html">Връщане и рекламации</a> · <a href="/terms.html">Общи условия</a> · <a href="/privacy.html">Поверителност</a> · <a href="/cookies.html">Бисквитки</a></div></div></footer>
              <script src="/i18n.js"></script>
              <script src="/consent.js"></script>
              {{(extraScript is null ? "" : $"<script>{extraScript}</script>")}}
            </body>
            </html>
            """;
    }
}

