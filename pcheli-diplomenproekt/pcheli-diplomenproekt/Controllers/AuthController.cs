using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;

namespace pcheli_diplomenproekt.Controllers;

/// <summary>Вход/изход на администратора чрез ASP.NET Core Identity (потребителите са в SQL Server базата).</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<IdentityUser> _signIn;
    private readonly UserManager<IdentityUser> _users;
    private readonly ILogger<AuthController> _logger;

    public AuthController(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users, ILogger<AuthController> logger)
    {
        _signIn = signIn; _users = users; _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _users.FindByNameAsync(dto.Username.Trim());
        if (user is null || !await _users.IsInRoleAsync(user, DbInitializer.AdminRole))
        {
            _logger.LogWarning("Неуспешен опит за вход ({User}) от {Ip}", dto.Username, HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(new { message = "Грешно потребителско име или парола." });
        }

        var result = await _signIn.PasswordSignInAsync(user, dto.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            _logger.LogWarning("Акаунтът {User} е временно заключен.", user.UserName);
            return Problem("Твърде много неуспешни опити. Опитайте отново след 10 минути.", statusCode: StatusCodes.Status423Locked);
        }
        if (!result.Succeeded)
        {
            _logger.LogWarning("Грешна парола за {User} от {Ip}", user.UserName, HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(new { message = "Грешно потребителско име или парола." });
        }

        _logger.LogInformation("Администратор {User} влезе в системата.", user.UserName);
        return Ok(new { username = user.UserName });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Ok();
    }

    [HttpGet("me")]
    [Authorize(Roles = DbInitializer.AdminRole)]
    public IActionResult Me() => Ok(new { username = User.Identity?.Name });
}
