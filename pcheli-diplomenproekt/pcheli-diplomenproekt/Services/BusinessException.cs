using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace pcheli_diplomenproekt.Services;

/// <summary>Очаквана бизнес грешка (невалидни данни, липсващ запис и т.н.) със съобщение за потребителя.</summary>
public class BusinessException : Exception
{
    public int StatusCode { get; }
    public BusinessException(string message, int statusCode = StatusCodes.Status400BadRequest) : base(message)
        => StatusCode = statusCode;
}

/// <summary>Глобална обработка на грешките във формат ProblemDetails.</summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        ProblemDetails problem;
        if (ex is BusinessException be)
        {
            problem = new ProblemDetails { Status = be.StatusCode, Title = be.Message };
        }
        else
        {
            _logger.LogError(ex, "Необработена грешка при {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Възникна вътрешна грешка. Опитайте отново по-късно."
            };
        }

        ctx.Response.StatusCode = problem.Status!.Value;
        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
