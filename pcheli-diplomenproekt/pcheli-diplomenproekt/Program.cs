using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Models;
using pcheli_diplomenproekt.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Microsoft SQL Server чрез Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Липсва ConnectionStrings:DefaultConnection в appsettings.json.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

// 2. Услуги (бизнес логика)
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IBeekeeperService, BeekeeperService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IPromoService, PromoService>();
builder.Services.AddScoped<IRegionService, RegionService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<AuditFilter>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IAccountSecurityService, AccountSecurityService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();
builder.Services.AddScoped<IStockAlertService, StockAlertService>();
builder.Services.AddScoped<IPaymentService, StripePaymentService>();
builder.Services.AddSingleton<IShippingService, ShippingService>();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("stripe", c => c.Timeout = TimeSpan.FromSeconds(20));

// Имейл известия: опашка + фонова услуга; SMTP само ако е конфигуриран (Email:Host), иначе писмата се записват в лога
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
if (!string.IsNullOrWhiteSpace(builder.Configuration["Email:Host"]))
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
else
    builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();
builder.Services.AddHostedService<EmailBackgroundService>();
builder.Services.AddHostedService<SubscriptionBackgroundService>();
builder.Services.AddHostedService<PendingPaymentCleanupService>();

// 3. Глобална обработка на грешки (ProblemDetails)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 4. ASP.NET Core Identity (потребители и роли в същата SQL Server база): администратори, клиенти и пчелари
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.User.RequireUniqueEmail = false; // уникалността на имейла се проверява при регистрация (администраторът може да е без имейл)
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<BulgarianIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

// API-то връща 401/403, а не пренасочване към страница за вход.
// SameSite=Lax: бисквитката се пази при връщане от плащане (Stripe), а междусайтовите POST заявки пак не я получават.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "pcheli_auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();

// 4б. Ограничаване на заявките по IP (защита от спам и груба сила). Изключва се с RateLimiting:Disabled=true.
var rateLimitDisabled = builder.Configuration.GetValue("RateLimiting:Disabled", false);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, ct) =>
    {
        if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
            ctx.HttpContext.Response.Headers.RetryAfter = ((int)retry.TotalSeconds).ToString();
        ctx.HttpContext.Response.ContentType = "application/problem+json";
        await ctx.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Твърде много заявки. Опитайте отново след малко."
        }, ct);
    };

    void Policy(string name, int permits, TimeSpan window) => options.AddPolicy(name, ctx =>
        rateLimitDisabled
            ? RateLimitPartition.GetNoLimiter("off")
            : RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 }));

    Policy("forms", 5, TimeSpan.FromMinutes(10));   // отзиви, заявки на пчелари
    Policy("auth", 10, TimeSpan.FromMinutes(5));    // вход, регистрация, смяна и възстановяване на парола
    Policy("orders", 20, TimeSpan.FromMinutes(10)); // поръчки
    Policy("track", 15, TimeSpan.FromMinutes(10));  // проследяване и плащане по номер на поръчка
    Policy("api", 120, TimeSpan.FromMinutes(1));    // оценка на цена, captcha, качване на файлове
});

// 5. CORS само за конфигурираните адреси (по подразбиране фронтендът се сервира от същия сървър и не се нуждае от CORS)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
    });
});

// 6. API контролери с JSON сериализация
builder.Services.AddControllers(options =>
    {
        // Съобщения на български за грешки при свързване на модела (напр. липсващо или невалидно тяло на заявката)
        var m = options.ModelBindingMessageProvider;
        m.SetValueIsInvalidAccessor(v => $"Стойността „{v}“ е невалидна.");
        m.SetValueMustNotBeNullAccessor(v => $"Полето „{v}“ е задължително.");
        m.SetAttemptedValueIsInvalidAccessor((v, f) => $"Стойността „{v}“ не е валидна за „{f}“.");
        m.SetMissingBindRequiredValueAccessor(f => $"Липсва стойност за „{f}“.");
        m.SetMissingKeyOrValueAccessor(() => "Липсва стойност.");
        m.SetMissingRequestBodyRequiredValueAccessor(() => "Липсва тяло на заявката.");
        m.SetNonPropertyAttemptedValueIsInvalidAccessor(v => $"Стойността „{v}“ е невалидна.");
        m.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Подадената стойност е невалидна.");
        m.SetUnknownValueIsInvalidAccessor(f => $"Подадената стойност за „{f}“ е невалидна.");
        m.SetValueMustBeANumberAccessor(f => $"Полето „{f}“ трябва да е число.");
        m.SetNonPropertyValueMustBeANumberAccessor(() => "Стойността трябва да е число.");
        options.ModelMetadataDetailsProviders.Add(new BulgarianValidationMetadataProvider());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Валидационните грешки излизат като ValidationProblemDetails с български заглавие
        options.InvalidModelStateResponseFactory = ctx =>
        {
            var problem = new ValidationProblemDetails(ctx.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Моля, коригирайте грешките във формата."
            };
            return new BadRequestObjectResult(problem);
        };
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 7. Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Пчелари България API",
        Version = "v1",
        Description = "RESTful Web API за националната платформа за обединяване на български пчелари."
    });
});

var app = builder.Build();

// Миграции + начални данни
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await DbInitializer.InitializeAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Грешка при инициализиране на базата данни. Проверете connection string-а и дали SQL Server/LocalDB работи.");
        throw;
    }
}

// IP адресът на клиента (за ограничаването на заявките и журнала) се взема от X-Forwarded-For само от доверени прокси (по подразбиране – локални)
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });

app.UseExceptionHandler();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Пчелари България API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseDefaultFiles();   // отваря wwwroot/index.html на "/"
app.UseStaticFiles(new StaticFileOptions
{
    // HTML, JS и CSS се проверяват при всяко зареждане (по ETag), за да не остава стара версия в кеша на браузъра.
    OnPrepareResponse = ctx =>
    {
        var ext = Path.GetExtension(ctx.File.Name);
        if (ext is ".html" or ".js" or ".css" or ".webmanifest")
            ctx.Context.Response.Headers.CacheControl = "no-cache";
    }
});

app.UseRouting();
app.UseRateLimiter();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Проверка за жизненост и връзка с базата (за мониторинг и балансьори). Не връща подробности за грешката.
app.MapGet("/health", async (ApplicationDbContext db, CancellationToken ct) =>
{
    try
    {
        return await db.Database.CanConnectAsync(ct)
            ? Results.Json(new { status = "Healthy" })
            : Results.Json(new { status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.Json(new { status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).ExcludeFromDescription();

app.Run();
