using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
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

// 3. Глобална обработка на грешки (ProblemDetails)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 4. ASP.NET Core Identity (потребители и роли в същата SQL Server база) + cookie за администраторите
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<BulgarianIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

// API-то връща 401/403, а не пренасочване към страница за вход
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "pcheli_admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();

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

app.UseExceptionHandler();

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
        if (ext is ".html" or ".js" or ".css")
            ctx.Context.Response.Headers.CacheControl = "no-cache";
    }
});

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
