using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

/// <summary>Клиентски акаунти: регистрация и вход, профил, история на поръчките, адреси, любими продукти и абонаменти.</summary>
[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IAccountService _account;
    private readonly ISubscriptionService _subscriptions;
    private readonly IOrderService _orders;
    private readonly IBeekeeperService _beekeepers;
    private readonly IAccountSecurityService _security;
    private readonly IConfiguration _config;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IAccountService account,
        ISubscriptionService subscriptions, IOrderService orders, IBeekeeperService beekeepers, IAccountSecurityService security,
        IConfiguration config, ILogger<AccountController> logger)
    {
        _users = users; _signIn = signIn; _account = account; _subscriptions = subscriptions;
        _orders = orders; _beekeepers = beekeepers; _security = security; _config = config; _logger = logger;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? throw new BusinessException("Нужен е вход.", StatusCodes.Status401Unauthorized);

    private string BaseUrl => (_config["App:PublicUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');

    private async Task<AccountDto> BuildAsync(ApplicationUser u) => new(u.Id, u.Email ?? u.UserName ?? string.Empty, u.FullName,
        u.PhoneNumber, (await _users.GetRolesAsync(u)).ToArray(), await _beekeepers.GetIdByUserAsync(u.Id), u.EmailConfirmed);

    // ---------- Вход и регистрация ----------
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [RequireCaptcha]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var email = dto.Email.Trim();
        if (await _users.FindByEmailAsync(email) != null)
            throw new BusinessException("Вече има акаунт с този имейл. Влезте или ползвайте друг имейл.", StatusCodes.Status409Conflict);

        var user = new ApplicationUser
        {
            UserName = email, Email = email, FullName = dto.FullName.Trim(), PhoneNumber = dto.Phone?.Trim(), EmailConfirmed = false
        };
        var result = await _users.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new BusinessException(string.Join(" ", result.Errors.Select(e => e.Description)));

        await _users.AddToRoleAsync(user, DbInitializer.CustomerRole);
        await _signIn.SignInAsync(user, isPersistent: true);
        _logger.LogInformation("Нов клиентски акаунт {Email}", email);
        try { await _security.SendConfirmationAsync(user.Id, BaseUrl); }
        catch (Exception ex) { _logger.LogWarning(ex, "Неуспешно изпращане на потвърждение на имейл"); }
        return Ok(await BuildAsync(user));
    }

    // ---------- Забравена парола и потвърждение на имейл ----------
    // POST: api/account/forgot-password – винаги един и същ отговор (не издава кои имейли имат акаунт)
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    [RequireCaptcha]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _security.RequestPasswordResetAsync(dto.Email, BaseUrl);
        return Ok(new { success = true, message = "Ако има акаунт с този имейл, ще получите писмо с връзка за нова парола." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await _security.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
        return Ok(new { success = true, message = "Паролата е сменена. Можете да влезете." });
    }

    [HttpPost("send-confirmation")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SendConfirmation()
    {
        await _security.SendConfirmationAsync(UserId, BaseUrl);
        return Ok(new { success = true, message = "Изпратихме писмо за потвърждение." });
    }

    [HttpPost("confirm-email")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
    {
        await _security.ConfirmEmailAsync(dto.Email, dto.Token);
        return Ok(new { success = true, message = "Имейлът е потвърден. Благодарим!" });
    }

    // ---------- Лични данни (GDPR) ----------
    // GET: api/account/export – всички данни на акаунта като JSON файл
    [HttpGet("export")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Export()
    {
        var data = await _security.ExportAsync(UserId);
        var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        return File(bytes, "application/json; charset=utf-8", "moite-danni.json");
    }

    // DELETE: api/account – изтрива акаунта след потвърждение с парола
    [HttpDelete]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto dto)
    {
        await _security.DeleteAccountAsync(UserId, dto.Password);
        await _signIn.SignOutAsync();
        _logger.LogInformation("Изтрит клиентски акаунт");
        return NoContent();
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] AccountLoginDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email.Trim()) ?? await _users.FindByNameAsync(dto.Email.Trim());
        // еднакъв отговор при непознат имейл и грешна парола
        const string bad = "Грешен имейл или парола.";
        if (user is null) return Unauthorized(new { message = bad });

        var result = await _signIn.PasswordSignInAsync(user, dto.Password, isPersistent: true, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            // дълго заключване = блокиран от администратор (а не заради грешни пароли)
            var blocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow.AddDays(30);
            return Problem(blocked ? "Акаунтът е блокиран. Свържете се с нас." : "Твърде много неуспешни опити. Опитайте отново след 10 минути.",
                statusCode: StatusCodes.Status423Locked);
        }
        if (!result.Succeeded) return Unauthorized(new { message = bad });
        return Ok(await BuildAsync(user));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout() { await _signIn.SignOutAsync(); return Ok(); }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _users.FindByIdAsync(UserId);
        if (user is null) { await _signIn.SignOutAsync(); return Unauthorized(); }
        return Ok(await BuildAsync(user));
    }

    // ---------- Профил ----------
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateDto dto)
    {
        var user = await _users.FindByIdAsync(UserId) ?? throw new BusinessException("Нужен е вход.", StatusCodes.Status401Unauthorized);
        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = dto.Phone?.Trim();
        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded) throw new BusinessException(string.Join(" ", result.Errors.Select(e => e.Description)));
        return Ok(await BuildAsync(user));
    }

    [HttpPost("password")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var user = await _users.FindByIdAsync(UserId) ?? throw new BusinessException("Нужен е вход.", StatusCodes.Status401Unauthorized);
        var result = await _users.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded) throw new BusinessException(string.Join(" ", result.Errors.Select(e => e.Description)));
        await _signIn.RefreshSignInAsync(user);
        return NoContent();
    }

    // ---------- Поръчки ----------
    [HttpGet("orders")]
    [Authorize]
    public async Task<IActionResult> MyOrders() => Ok(await _orders.GetForUserAsync(UserId));

    // POST: api/account/orders/BG-261118-1234/cancel – отказ, докато поръчката е само „Приета“
    [HttpPost("orders/{number}/cancel")]
    [Authorize]
    [EnableRateLimiting("track")]
    public async Task<IActionResult> CancelOrder(string number, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] CancelReasonDto? dto) =>
        Ok(await _orders.CancelByCustomerAsync(number, null, UserId, dto?.Reason));

    // GET: api/account/orders/BG-261118-1234/receipt – разписка за печат
    [HttpGet("orders/{number}/receipt")]
    [Authorize]
    public async Task<IActionResult> Receipt(string number) =>
        Content(OrderExport.ReceiptHtml(await _orders.FindForCustomerAsync(number, null, UserId), ShopInfo.From(_config)), "text/html; charset=utf-8");

    // ---------- Адреси ----------
    [HttpGet("addresses")]
    [Authorize]
    public async Task<IActionResult> Addresses() => Ok(await _account.GetAddressesAsync(UserId));

    [HttpPost("addresses")]
    [Authorize]
    public async Task<IActionResult> AddAddress([FromBody] AddressUpsertDto dto) => Ok(await _account.AddAddressAsync(UserId, dto));

    [HttpPut("addresses/{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateAddress(int id, [FromBody] AddressUpsertDto dto) => Ok(await _account.UpdateAddressAsync(UserId, id, dto));

    [HttpDelete("addresses/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteAddress(int id) { await _account.DeleteAddressAsync(UserId, id); return NoContent(); }

    // ---------- Любими ----------
    [HttpGet("wishlist")]
    [Authorize]
    public async Task<IActionResult> Wishlist() => Ok(await _account.GetWishlistAsync(UserId));

    [HttpGet("wishlist/ids")]
    [Authorize]
    public async Task<IActionResult> WishlistIds() => Ok(await _account.GetWishlistIdsAsync(UserId));

    [HttpPut("wishlist/{productId:int}")]
    [Authorize]
    public async Task<IActionResult> AddWish(int productId) { await _account.AddToWishlistAsync(UserId, productId); return NoContent(); }

    [HttpDelete("wishlist/{productId:int}")]
    [Authorize]
    public async Task<IActionResult> RemoveWish(int productId) { await _account.RemoveFromWishlistAsync(UserId, productId); return NoContent(); }

    [HttpPost("wishlist/merge")]
    [Authorize]
    public async Task<IActionResult> MergeWishlist([FromBody] WishlistMergeDto dto)
    {
        await _account.MergeWishlistAsync(UserId, dto.ProductIds);
        return Ok(await _account.GetWishlistIdsAsync(UserId));
    }

    // ---------- Абонаменти ----------
    [HttpGet("subscriptions")]
    [Authorize]
    public async Task<IActionResult> Subscriptions() => Ok(await _subscriptions.GetForUserAsync(UserId));

    [HttpPost("subscriptions")]
    [Authorize]
    public async Task<IActionResult> CreateSubscription([FromBody] SubscriptionCreateDto dto) => Ok(await _subscriptions.CreateAsync(UserId, dto));

    [HttpPost("subscriptions/{id:int}/status")]
    [Authorize]
    public async Task<IActionResult> SubscriptionStatus(int id, [FromBody] SubscriptionStatusDto dto) =>
        Ok(await _subscriptions.SetStatusAsync(UserId, id, dto.Action));
}
