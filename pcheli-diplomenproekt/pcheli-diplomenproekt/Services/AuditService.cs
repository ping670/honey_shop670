using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

public interface IAuditService
{
    Task LogAsync(AuditLog entry);
    Task<PagedAuditDto> GetAsync(int page, int pageSize, string? user, string? search);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    public AuditService(ApplicationDbContext db) => _db = db;

    public async Task LogAsync(AuditLog entry)
    {
        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task<PagedAuditDto> GetAsync(int page, int pageSize, string? user, string? search)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        var q = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(user)) q = q.Where(a => a.UserName == user.Trim());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(a => a.Action.Contains(s) || a.Path.Contains(s) || (a.Details != null && a.Details.Contains(s)) || (a.EntityId != null && a.EntityId == s));
        }
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(a => a.At).ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogDto(a.Id, a.At, a.UserName, a.Method, a.Action, a.Path, a.EntityId, a.StatusCode, a.Ip, a.Details))
            .ToListAsync();
        return new PagedAuditDto(items, total);
    }
}

public static class AuditExtensions
{
    public const string DetailsKey = "audit.details";

    /// <summary>Добавя пояснение към записа в журнала за текущата заявка (напр. новия статус на поръчка).</summary>
    public static void AuditDetails(this ControllerBase c, string details) => c.HttpContext.Items[DetailsKey] = details;
}

/// <summary>
/// Записва в журнала всяка променяща заявка (POST/PUT/PATCH/DELETE) и експортите, направени от администратор или пчелар:
/// кой, какво, кога, от кой IP и с какъв резултат. Тялото на заявката не се записва (може да съдържа лични данни).
/// </summary>
public class AuditFilter : IAsyncActionFilter
{
    private readonly IAuditService _audit;
    private readonly ILogger<AuditFilter> _logger;
    public AuditFilter(IAuditService audit, ILogger<AuditFilter> logger) { _audit = audit; _logger = logger; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        var http = context.HttpContext;
        var method = http.Request.Method;
        var isExport = http.Request.Path.Value?.Contains("/export", StringComparison.OrdinalIgnoreCase) == true;
        if (HttpMethods.IsGet(method) && !isExport) return;
        if (HttpMethods.IsHead(method) || HttpMethods.IsOptions(method)) return;

        try
        {
            var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var status = executed.Exception != null && !executed.ExceptionHandled
                ? (executed.Exception is BusinessException be ? be.StatusCode : 500)
                : (executed.Result as IStatusCodeActionResult)?.StatusCode ?? http.Response.StatusCode;

            context.RouteData.Values.TryGetValue("id", out var id);
            await _audit.LogAsync(new AuditLog
            {
                At = DateTime.UtcNow,
                UserName = http.User.Identity?.Name ?? "—",
                Method = method,
                Action = $"{descriptor?.ControllerName}/{descriptor?.ActionName}",
                Path = http.Request.Path.Value?[..Math.Min(200, http.Request.Path.Value.Length)] ?? string.Empty,
                EntityId = id?.ToString(),
                StatusCode = status,
                Ip = http.Connection.RemoteIpAddress?.ToString(),
                Details = http.Items[AuditExtensions.DetailsKey] as string
            });
        }
        catch (Exception ex)
        {
            // Журналът никога не бива да чупи самата операция
            _logger.LogError(ex, "Неуспешен запис в журнала на действията");
        }
    }
}
