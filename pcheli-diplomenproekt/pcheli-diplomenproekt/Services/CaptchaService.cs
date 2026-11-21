using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace pcheli_diplomenproekt.Services;

public record CaptchaChallenge(string Token, string Question);

public interface ICaptchaService
{
    bool Enabled { get; }
    CaptchaChallenge Create();
    bool Verify(string? token, string? answer);
}

/// <summary>
/// Вградена проверка „не съм робот“ без външни услуги: сървърът издава малка задача, а отговорът е
/// в подписан, ограничен във времето токен. Всеки токен е за еднократна употреба и не може да бъде
/// изпратен по-рано от 2 секунди след издаването (спира наивните ботове). Изключва се с Captcha:Enabled=false.
/// </summary>
public class CaptchaService : ICaptchaService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(20);
    private readonly ITimeLimitedDataProtector _protector;
    private readonly IMemoryCache _used;
    private readonly int _minSeconds;

    public bool Enabled { get; }

    public CaptchaService(IDataProtectionProvider provider, IMemoryCache cache, IConfiguration config)
    {
        _protector = provider.CreateProtector("pcheli.captcha.v1").ToTimeLimitedDataProtector();
        _used = cache;
        Enabled = config.GetValue("Captcha:Enabled", true);
        _minSeconds = config.GetValue("Captcha:MinSeconds", 2);
    }

    public CaptchaChallenge Create()
    {
        int a = RandomNumberGenerator.GetInt32(2, 10), b = RandomNumberGenerator.GetInt32(2, 10);
        var (question, answer) = RandomNumberGenerator.GetInt32(0, 3) switch
        {
            0 => ($"Колко е {a} + {b}?", a + b),
            1 => ($"Колко е {a + b} − {b}?", a),
            _ => ($"Колко е {a} × {b}?", a * b)
        };
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
        var payload = $"{answer}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}|{nonce}";
        return new CaptchaChallenge(_protector.Protect(payload, Lifetime), question);
    }

    public bool Verify(string? token, string? answer)
    {
        if (!Enabled) return true;
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(answer)) return false;

        string payload;
        try { payload = _protector.Unprotect(token); }
        catch (CryptographicException) { return false; }   // подправен или изтекъл токен

        var parts = payload.Split('|');
        if (parts.Length != 3 || !long.TryParse(parts[1], out var issued)) return false;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - issued < _minSeconds) return false;
        if (!string.Equals(parts[0], answer.Trim(), StringComparison.Ordinal)) return false;

        // еднократна употреба
        if (_used.TryGetValue(parts[2], out _)) return false;
        _used.Set(parts[2], true, Lifetime);
        return true;
    }
}

/// <summary>Изисква валиден отговор на задачата в хедърите X-Captcha-Token и X-Captcha-Answer.</summary>
public class RequireCaptchaAttribute : TypeFilterAttribute
{
    public RequireCaptchaAttribute() : base(typeof(CaptchaFilter)) { }
}

public class CaptchaFilter : IAsyncActionFilter
{
    private readonly ICaptchaService _captcha;
    public CaptchaFilter(ICaptchaService captcha) => _captcha = captcha;

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var h = context.HttpContext.Request.Headers;
        if (!_captcha.Verify(h["X-Captcha-Token"].FirstOrDefault(), h["X-Captcha-Answer"].FirstOrDefault()))
            throw new BusinessException("Проверката „не съм робот“ е неуспешна или е изтекла. Опитайте отново.", StatusCodes.Status400BadRequest);
        return next();
    }
}
