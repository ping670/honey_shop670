using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

public interface IArticleService
{
    Task<(List<ArticleListDto> Items, int Total)> GetPublishedAsync(int page, int pageSize, string? category);
    Task<ArticleDto> GetBySlugAsync(string slug, bool includeUnpublished = false);
    Task<List<ArticleDto>> GetAllAsync();
    Task<ArticleDto> CreateAsync(ArticleUpsertDto dto);
    Task<ArticleDto> UpdateAsync(int id, ArticleUpsertDto dto);
    Task DeleteAsync(int id);
}

public class ArticleService : IArticleService
{
    private readonly ApplicationDbContext _db;
    public ArticleService(ApplicationDbContext db) => _db = db;

    private static ArticleDto ToDto(Article a) => new(a.Id, a.Slug, a.Title, a.Summary, a.Content, a.CoverImage,
        a.Author, a.Category, a.IsPublished, a.PublishedAt);

    public async Task<(List<ArticleListDto> Items, int Total)> GetPublishedAsync(int page, int pageSize, string? category)
    {
        var q = _db.Articles.AsNoTracking().Where(a => a.IsPublished && a.PublishedAt <= DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(category) && category != "all") q = q.Where(a => a.Category == category);
        var total = await q.CountAsync();
        pageSize = Math.Clamp(pageSize, 1, 50);
        var items = await q.OrderByDescending(a => a.PublishedAt).ThenByDescending(a => a.Id)
            .Skip((Math.Max(1, page) - 1) * pageSize).Take(pageSize)
            .Select(a => new ArticleListDto(a.Id, a.Slug, a.Title, a.Summary, a.CoverImage, a.Author, a.Category, a.PublishedAt))
            .ToListAsync();
        return (items, total);
    }

    public async Task<ArticleDto> GetBySlugAsync(string slug, bool includeUnpublished = false)
    {
        var a = await _db.Articles.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && (includeUnpublished || x.IsPublished))
                ?? throw new BusinessException("Статията не е намерена.", StatusCodes.Status404NotFound);
        return ToDto(a);
    }

    public async Task<List<ArticleDto>> GetAllAsync() =>
        (await _db.Articles.AsNoTracking().OrderByDescending(a => a.PublishedAt).ToListAsync()).Select(ToDto).ToList();

    public async Task<ArticleDto> CreateAsync(ArticleUpsertDto dto)
    {
        UrlRules.Require(dto.CoverImage, "Заглавна снимка");
        var a = new Article { PublishedAt = DateTime.UtcNow };
        Apply(a, dto);
        a.Slug = await UniqueSlugAsync(string.IsNullOrWhiteSpace(dto.Slug) ? dto.Title : dto.Slug, null);
        _db.Articles.Add(a);
        await _db.SaveChangesAsync();
        return ToDto(a);
    }

    public async Task<ArticleDto> UpdateAsync(int id, ArticleUpsertDto dto)
    {
        UrlRules.Require(dto.CoverImage, "Заглавна снимка");
        var a = await _db.Articles.FindAsync(id) ?? throw new BusinessException("Статията не е намерена.", StatusCodes.Status404NotFound);
        Apply(a, dto);
        if (!string.IsNullOrWhiteSpace(dto.Slug) && dto.Slug.Trim() != a.Slug)
            a.Slug = await UniqueSlugAsync(dto.Slug, id);
        await _db.SaveChangesAsync();
        return ToDto(a);
    }

    public async Task DeleteAsync(int id)
    {
        var a = await _db.Articles.FindAsync(id) ?? throw new BusinessException("Статията не е намерена.", StatusCodes.Status404NotFound);
        _db.Articles.Remove(a);
        await _db.SaveChangesAsync();
    }

    private static void Apply(Article a, ArticleUpsertDto d)
    {
        a.Title = d.Title.Trim(); a.Summary = d.Summary.Trim(); a.Content = d.Content.Trim();
        a.CoverImage = string.IsNullOrWhiteSpace(d.CoverImage) ? "./images/hero.png" : d.CoverImage.Trim();
        a.Author = d.Author.Trim(); a.Category = d.Category.Trim(); a.IsPublished = d.IsPublished;
    }

    private async Task<string> UniqueSlugAsync(string source, int? ignoreId)
    {
        var baseSlug = Slugify(source);
        var slug = baseSlug;
        for (var i = 2; await _db.Articles.AnyAsync(a => a.Slug == slug && a.Id != ignoreId); i++)
            slug = $"{baseSlug}-{i}";
        return slug;
    }

    // ---------- Slug ----------
    private static readonly Dictionary<char, string> Translit = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ж'] = "zh", ['з'] = "z", ['и'] = "i",
        ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n", ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s",
        ['т'] = "t", ['у'] = "u", ['ф'] = "f", ['х'] = "h", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "sht",
        ['ъ'] = "a", ['ь'] = "y", ['ю'] = "yu", ['я'] = "ya"
    };

    public static string Slugify(string text)
    {
        var sb = new StringBuilder();
        foreach (var ch in text.Trim().ToLowerInvariant())
        {
            if (Translit.TryGetValue(ch, out var t)) sb.Append(t);
            else if (ch is >= 'a' and <= 'z' or >= '0' and <= '9') sb.Append(ch);
            else sb.Append('-');
        }
        var slug = Regex.Replace(sb.ToString(), "-{2,}", "-").Trim('-');
        if (slug.Length > 100) slug = slug[..100].Trim('-');
        return slug.Length == 0 ? "statia" : slug;
    }
}

/// <summary>Превръща текста на статия в безопасен HTML: всичко се кодира, а само абзаци, „## “ подзаглавия и „- “ списъци са позволени.</summary>
public static class ArticleRenderer
{
    public static string ToHtml(string content)
    {
        var enc = HtmlText.Encoder;
        var sb = new StringBuilder();
        var inList = false;
        var paragraph = new List<string>();

        void FlushParagraph()
        {
            if (paragraph.Count == 0) return;
            sb.Append("<p>").Append(string.Join("<br>", paragraph.Select(enc.Encode))).Append("</p>\n");
            paragraph.Clear();
        }
        void CloseList() { if (inList) { sb.Append("</ul>\n"); inList = false; } }

        foreach (var raw in content.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) { FlushParagraph(); CloseList(); continue; }
            if (line.StartsWith("## ")) { FlushParagraph(); CloseList(); sb.Append("<h2>").Append(enc.Encode(line[3..])).Append("</h2>\n"); }
            else if (line.StartsWith("- "))
            {
                FlushParagraph();
                if (!inList) { sb.Append("<ul>\n"); inList = true; }
                sb.Append("<li>").Append(enc.Encode(line[2..])).Append("</li>\n");
            }
            else { CloseList(); paragraph.Add(line); }
        }
        FlushParagraph(); CloseList();
        return sb.ToString();
    }
}
