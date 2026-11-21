using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

public interface IAccountSecurityService
{
    /// <summary>Изпраща връзка за нова парола. Никога не разкрива дали имейлът има акаунт.</summary>
    Task RequestPasswordResetAsync(string email, string baseUrl);
    Task ResetPasswordAsync(string email, string token, string newPassword);
    Task SendConfirmationAsync(string userId, string baseUrl);
    Task ConfirmEmailAsync(string email, string token);
    /// <summary>Изтрива акаунта и личните му данни (GDPR). Историята на поръчките се пази анонимно за счетоводството.</summary>
    Task DeleteAccountAsync(string userId, string password);
    Task<object> ExportAsync(string userId);
}

public class AccountSecurityService : IAccountSecurityService
{
    private const string InvalidLink = "Връзката е невалидна или е изтекла. Заявете нова.";

    private readonly UserManager<ApplicationUser> _users;
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notify;

    public AccountSecurityService(UserManager<ApplicationUser> users, ApplicationDbContext db, INotificationService notify)
    { _users = users; _db = db; _notify = notify; }

    private static string Link(string baseUrl, string kind, string email, string token) =>
        $"{baseUrl.TrimEnd('/')}/account.html?{kind}=1&email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

    public async Task RequestPasswordResetAsync(string email, string baseUrl)
    {
        var user = await _users.FindByEmailAsync(email.Trim());
        if (user is null || string.IsNullOrWhiteSpace(user.Email)) return;
        var token = await _users.GeneratePasswordResetTokenAsync(user);
        _notify.PasswordReset(user.Email, user.FullName, Link(baseUrl, "reset", user.Email, token));
    }

    public async Task ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _users.FindByEmailAsync(email.Trim()) ?? throw new BusinessException(InvalidLink);
        var result = await _users.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == "InvalidToken")) throw new BusinessException(InvalidLink);
            throw new BusinessException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
        // след успешна смяна акаунтът се отключва (ако е бил заключен заради грешни пароли)
        await _users.SetLockoutEndDateAsync(user, null);
        await _users.ResetAccessFailedCountAsync(user);
    }

    public async Task SendConfirmationAsync(string userId, string baseUrl)
    {
        var user = await _users.FindByIdAsync(userId) ?? throw new BusinessException("Нужен е вход.", StatusCodes.Status401Unauthorized);
        if (string.IsNullOrWhiteSpace(user.Email)) throw new BusinessException("Акаунтът няма имейл.");
        if (user.EmailConfirmed) return;
        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        _notify.EmailConfirmation(user.Email, user.FullName, Link(baseUrl, "confirm", user.Email, token));
    }

    public async Task ConfirmEmailAsync(string email, string token)
    {
        var user = await _users.FindByEmailAsync(email.Trim()) ?? throw new BusinessException(InvalidLink);
        if (user.EmailConfirmed) return;
        var result = await _users.ConfirmEmailAsync(user, token);
        if (!result.Succeeded) throw new BusinessException(InvalidLink);
    }

    public async Task DeleteAccountAsync(string userId, string password)
    {
        var user = await _users.FindByIdAsync(userId) ?? throw new BusinessException("Нужен е вход.", StatusCodes.Status401Unauthorized);
        if (!await _users.CheckPasswordAsync(user, password))
            throw new BusinessException("Грешна парола.", StatusCodes.Status400BadRequest);
        if (await _users.IsInRoleAsync(user, DbInitializer.AdminRole))
            throw new BusinessException("Администраторски акаунт не може да бъде изтрит оттук.", StatusCodes.Status403Forbidden);
        if (await _db.Beekeepers.AnyAsync(b => b.UserId == userId))
            throw new BusinessException("Акаунтът е свързан с пчелин. Свържете се с администратор, за да го отвържем преди изтриване.", StatusCodes.Status409Conflict);

        await using var tx = await _db.Database.BeginTransactionAsync();
        await _db.SavedAddresses.Where(a => a.UserId == userId).ExecuteDeleteAsync();
        await _db.WishlistItems.Where(w => w.UserId == userId).ExecuteDeleteAsync();
        await _db.Subscriptions.Where(s => s.UserId == userId).ExecuteDeleteAsync();
        await _db.BeekeeperApplications.Where(a => a.UserId == userId).ExecuteUpdateAsync(s => s.SetProperty(a => a.UserId, (string?)null));
        // поръчките се пазят за счетоводството (срок по закон), но се отвързват от акаунта
        await _db.Orders.Where(o => o.UserId == userId).ExecuteUpdateAsync(s => s.SetProperty(o => o.UserId, (string?)null));

        var result = await _users.DeleteAsync(user);
        if (!result.Succeeded) throw new BusinessException(string.Join(" ", result.Errors.Select(e => e.Description)));
        await tx.CommitAsync();
    }

    public async Task<object> ExportAsync(string userId)
    {
        var user = await _users.FindByIdAsync(userId) ?? throw new BusinessException("Нужен е вход.", StatusCodes.Status401Unauthorized);
        var addresses = await _db.SavedAddresses.AsNoTracking().Where(a => a.UserId == userId)
            .Select(a => new { a.Label, a.FullName, a.Phone, a.City, a.Address, a.IsDefault }).ToListAsync();
        var wishlist = await _db.WishlistItems.AsNoTracking().Where(w => w.UserId == userId)
            .Select(w => new { Product = w.Product!.Name, w.AddedAt }).ToListAsync();
        var subscriptions = await _db.Subscriptions.AsNoTracking().Where(s => s.UserId == userId)
            .Select(s => new { Product = s.ProductVariant!.Product!.Name, Pack = s.ProductVariant.Label, s.Quantity, s.IntervalMonths,
                s.NextDeliveryDate, s.Status, s.DeliveryCity, s.DeliveryAddress, s.CreatedAt }).ToListAsync();
        var orders = (await _db.Orders.AsNoTracking().Include(o => o.Items).Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt).ToListAsync()).Select(o => o.ToDto()).ToList();

        return new
        {
            ExportedAt = DateTime.UtcNow,
            Account = new { user.Email, user.FullName, Phone = user.PhoneNumber, user.EmailConfirmed, user.CreatedAt },
            Addresses = addresses, Wishlist = wishlist, Subscriptions = subscriptions, Orders = orders
        };
    }
}

// ============================ Потребители (администрация) ============================
public interface IUserAdminService
{
    Task<PagedUsersDto> ListAsync(string? search, int page, int pageSize);
    Task<AdminUserDto> SetLockedAsync(string id, bool locked, string? actingUserId);
}

public class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ApplicationDbContext _db;
    public UserAdminService(UserManager<ApplicationUser> users, ApplicationDbContext db) { _users = users; _db = db; }

    private async Task<AdminUserDto> ToDtoAsync(ApplicationUser u) => new(u.Id, u.UserName ?? string.Empty, u.Email ?? string.Empty,
        u.FullName, (await _users.GetRolesAsync(u)).ToArray(), u.EmailConfirmed,
        u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow, u.CreatedAt,
        await _db.Orders.CountAsync(o => o.UserId == u.Id));

    public async Task<PagedUsersDto> ListAsync(string? search, int page, int pageSize)
    {
        var q = _users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(u => (u.Email != null && u.Email.Contains(s)) || (u.UserName != null && u.UserName.Contains(s)) || u.FullName.Contains(s));
        }
        var total = await q.CountAsync();
        var size = Math.Clamp(pageSize, 1, 100);
        var list = await q.OrderByDescending(u => u.CreatedAt).Skip((Math.Max(1, page) - 1) * size).Take(size).ToListAsync();
        var items = new List<AdminUserDto>();
        foreach (var u in list) items.Add(await ToDtoAsync(u));
        return new PagedUsersDto(items, total);
    }

    /// <summary>Блокира (до 100 години) или отблокира акаунт. Администратор и собственият акаунт не могат да бъдат блокирани.</summary>
    public async Task<AdminUserDto> SetLockedAsync(string id, bool locked, string? actingUserId)
    {
        var u = await _users.FindByIdAsync(id) ?? throw new BusinessException("Потребителят не е намерен.", StatusCodes.Status404NotFound);
        if (locked)
        {
            if (u.Id == actingUserId) throw new BusinessException("Не можете да блокирате собствения си акаунт.");
            if (await _users.IsInRoleAsync(u, DbInitializer.AdminRole))
                throw new BusinessException("Администраторски акаунт не може да бъде блокиран.", StatusCodes.Status403Forbidden);
            await _users.SetLockoutEnabledAsync(u, true);
            await _users.SetLockoutEndDateAsync(u, DateTimeOffset.UtcNow.AddYears(100));
        }
        else
        {
            await _users.SetLockoutEndDateAsync(u, null);
            await _users.ResetAccessFailedCountAsync(u);
        }
        return await ToDtoAsync(u);
    }
}
