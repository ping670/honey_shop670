using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Models;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Tests;

/// <summary>Забравена парола, потвърждение на имейл, изтриване и износ на акаунт, блокиране – с истински Identity (SQLite в паметта).</summary>
public sealed class AccountSecurityTests : IDisposable
{
    private const string Password = "Abcdef1!xyz";
    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    private readonly ServiceProvider _sp;
    private readonly FakeNotifier _notifier = new();
    private readonly IServiceScope _scope;

    public AccountSecurityTests()
    {
        _conn.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(_conn));
        services.AddIdentityCore<ApplicationUser>(o => { o.Password.RequiredLength = 8; o.User.RequireUniqueEmail = false; })
            .AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        services.AddSingleton<INotificationService>(_notifier);
        services.AddScoped<IAccountSecurityService, AccountSecurityService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        _sp = services.BuildServiceProvider();
        _scope = _sp.CreateScope();
        Db.Database.EnsureCreated();
    }

    private ApplicationDbContext Db => _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    private UserManager<ApplicationUser> Users => _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    private IAccountSecurityService Security => _scope.ServiceProvider.GetRequiredService<IAccountSecurityService>();

    private async Task<ApplicationUser> CreateUserAsync(string email = "klient@example.com")
    {
        var u = new ApplicationUser { UserName = email, Email = email, FullName = "Клиент Тестов" };
        var r = await Users.CreateAsync(u, Password);
        Assert.True(r.Succeeded, string.Join(";", r.Errors.Select(e => e.Description)));
        return u;
    }

    private static string TokenFrom(string link) =>
        Uri.UnescapeDataString(link.Split("token=")[1]);

    // ---------- Забравена парола ----------
    [Fact]
    public async Task ForgotPassword_SendsLink_ThatResetsPassword_AndUnlocksAccount()
    {
        var u = await CreateUserAsync();
        await Users.SetLockoutEndDateAsync(u, DateTimeOffset.UtcNow.AddMinutes(10));

        await Security.RequestPasswordResetAsync("KLIENT@example.com", "https://example.com");

        var (email, link) = Assert.Single(_notifier.PasswordResets);
        Assert.Equal("klient@example.com", email);
        Assert.StartsWith("https://example.com/account.html?reset=1&email=klient%40example.com&token=", link);

        await Security.ResetPasswordAsync("klient@example.com", TokenFrom(link), "NovaParola1!x");

        var fresh = (await Users.FindByIdAsync(u.Id))!;
        Assert.True(await Users.CheckPasswordAsync(fresh, "NovaParola1!x"));
        Assert.False(await Users.CheckPasswordAsync(fresh, Password));
        Assert.Null(fresh.LockoutEnd);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_SendsNothing_AndDoesNotThrow()
    {
        await Security.RequestPasswordResetAsync("nikoi@example.com", "https://example.com");
        Assert.Empty(_notifier.PasswordResets);
    }

    [Fact]
    public async Task ResetPassword_WithBadToken_Fails_AndWeakPasswordIsExplained()
    {
        var u = await CreateUserAsync();
        await Assert.ThrowsAsync<BusinessException>(() => Security.ResetPasswordAsync("klient@example.com", "negoden-token", "NovaParola1!x"));
        await Assert.ThrowsAsync<BusinessException>(() => Security.ResetPasswordAsync("nikoi@example.com", "x", "NovaParola1!x"));

        await Security.RequestPasswordResetAsync("klient@example.com", "https://example.com");
        var token = TokenFrom(_notifier.PasswordResets.Single().Link);
        var ex = await Assert.ThrowsAsync<BusinessException>(() => Security.ResetPasswordAsync("klient@example.com", token, "123"));
        Assert.DoesNotContain("изтекла", ex.Message);                // грешката е за паролата, не за връзката
        Assert.True(await Users.CheckPasswordAsync((await Users.FindByIdAsync(u.Id))!, Password));
    }

    [Fact]
    public async Task ResetToken_WorksOnlyOnce()
    {
        await CreateUserAsync();
        await Security.RequestPasswordResetAsync("klient@example.com", "https://example.com");
        var token = TokenFrom(_notifier.PasswordResets.Single().Link);
        await Security.ResetPasswordAsync("klient@example.com", token, "NovaParola1!x");
        await Assert.ThrowsAsync<BusinessException>(() => Security.ResetPasswordAsync("klient@example.com", token, "DrugaParola1!x"));
    }

    // ---------- Потвърждение на имейл ----------
    [Fact]
    public async Task EmailConfirmation_LinkConfirmsAccount()
    {
        var u = await CreateUserAsync();
        await Security.SendConfirmationAsync(u.Id, "https://example.com");
        var (_, link) = Assert.Single(_notifier.Confirmations);
        Assert.Contains("confirm=1", link);

        await Assert.ThrowsAsync<BusinessException>(() => Security.ConfirmEmailAsync("klient@example.com", "negoden"));
        Assert.False((await Users.FindByIdAsync(u.Id))!.EmailConfirmed);

        await Security.ConfirmEmailAsync("klient@example.com", TokenFrom(link));
        Assert.True((await Users.FindByIdAsync(u.Id))!.EmailConfirmed);

        await Security.SendConfirmationAsync(u.Id, "https://example.com");   // вече потвърден – без ново писмо
        Assert.Single(_notifier.Confirmations);
    }

    // ---------- Изтриване и износ ----------
    private async Task<(ApplicationUser User, int OrderId)> SeedUserDataAsync()
    {
        var u = await CreateUserAsync();
        var db = Db;
        var bk = new Beekeeper { FullName = "П", ApiaryName = "Пчелин", RegistrationNumber = "R1", RegionId = 1, CityOrVillage = "с", Phone = "1" };
        db.Beekeepers.Add(bk); db.SaveChanges();
        var p = new Product { BeekeeperId = bk.Id, Name = "Мед", Type = "Мед", Category = "herbal", Origin = "Тракия",
            Variants = { new ProductVariant { Label = "0.5 кг", Weight = "0.5kg", Price = 10m } } };
        db.Products.Add(p); db.SaveChanges();
        db.SavedAddresses.Add(new SavedAddress { UserId = u.Id, FullName = "К", Phone = "1", City = "София", Address = "ул. 1" });
        db.WishlistItems.Add(new WishlistItem { UserId = u.Id, ProductId = p.Id });
        db.Subscriptions.Add(new Subscription { UserId = u.Id, ProductVariantId = p.Variants.First().Id, CustomerName = "К", CustomerPhone = "1", DeliveryCity = "С", DeliveryAddress = "а", NextDeliveryDate = DateTime.UtcNow.AddDays(7) });
        var o = new Order { OrderNumber = "BG-TEST-0001", CustomerName = "Клиент", CustomerPhone = "0888111222", DeliveryCity = "София", DeliveryAddress = "ул. 1", UserId = u.Id, TotalAmount = 10m };
        o.Items.Add(new OrderItem { ProductId = p.Id, ProductVariantId = p.Variants.First().Id, ProductName = "Мед", Quantity = 1, UnitPrice = 10m, TotalPrice = 10m });
        db.Orders.Add(o); db.SaveChanges();
        return (u, o.Id);
    }

    [Fact]
    public async Task DeleteAccount_RemovesPersonalData_KeepsOrdersAnonymously()
    {
        var (u, orderId) = await SeedUserDataAsync();

        await Assert.ThrowsAsync<BusinessException>(() => Security.DeleteAccountAsync(u.Id, "greshna-parola"));
        Assert.NotNull(await Users.FindByIdAsync(u.Id));

        await Security.DeleteAccountAsync(u.Id, Password);

        Assert.Null(await Users.FindByIdAsync(u.Id));
        var db = Db;
        Assert.Empty(db.SavedAddresses);
        Assert.Empty(db.WishlistItems);
        Assert.Empty(db.Subscriptions);
        var order = db.Orders.AsNoTracking().Single(o => o.Id == orderId);
        Assert.Null(order.UserId);                                  // отвързана, но запазена за счетоводството
    }

    [Fact]
    public async Task DeleteAccount_IsRefusedForAdminsAndLinkedBeekeepers()
    {
        var admin = await CreateUserAsync("admin@example.com");
        await _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>().CreateAsync(new IdentityRole(DbInitializer.AdminRole));
        await Users.AddToRoleAsync(admin, DbInitializer.AdminRole);
        await Assert.ThrowsAsync<BusinessException>(() => Security.DeleteAccountAsync(admin.Id, Password));

        var bkUser = await CreateUserAsync("pchelar@example.com");
        Db.Beekeepers.Add(new Beekeeper { FullName = "П", ApiaryName = "П", RegistrationNumber = "R9", RegionId = 1, CityOrVillage = "с", Phone = "1", UserId = bkUser.Id });
        Db.SaveChanges();
        var ex = await Assert.ThrowsAsync<BusinessException>(() => Security.DeleteAccountAsync(bkUser.Id, Password));
        Assert.Equal(409, ex.StatusCode);
        Assert.NotNull(await Users.FindByIdAsync(bkUser.Id));
    }

    [Fact]
    public async Task Export_ContainsOnlyOwnData()
    {
        var (u, _) = await SeedUserDataAsync();
        var other = await CreateUserAsync("drug@example.com");
        Db.SavedAddresses.Add(new SavedAddress { UserId = other.Id, FullName = "ЧУЖД", Phone = "2", City = "Варна", Address = "ул. 2" });
        Db.SaveChanges();

        var json = System.Text.Json.JsonSerializer.Serialize(await Security.ExportAsync(u.Id),
            new System.Text.Json.JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        Assert.Contains("klient@example.com", json);
        Assert.Contains("BG-TEST-0001", json);
        Assert.Contains("София", json);
        Assert.DoesNotContain("ЧУЖД", json);
        Assert.DoesNotContain("Варна", json);
        Assert.DoesNotContain("PasswordHash", json);
    }

    // ---------- Блокиране от администратор ----------
    [Fact]
    public async Task AdminCanLockAndUnlock_ButNotAdminsOrSelf()
    {
        var admin = await CreateUserAsync("admin@example.com");
        await _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>().CreateAsync(new IdentityRole(DbInitializer.AdminRole));
        await Users.AddToRoleAsync(admin, DbInitializer.AdminRole);
        var user = await CreateUserAsync();
        var svc = _scope.ServiceProvider.GetRequiredService<IUserAdminService>();

        Assert.True((await svc.SetLockedAsync(user.Id, true, admin.Id)).IsLocked);
        Assert.True(await Users.IsLockedOutAsync((await Users.FindByIdAsync(user.Id))!));
        Assert.False((await svc.SetLockedAsync(user.Id, false, admin.Id)).IsLocked);

        await Assert.ThrowsAsync<BusinessException>(() => svc.SetLockedAsync(admin.Id, true, admin.Id));
        await Assert.ThrowsAsync<BusinessException>(() => svc.SetLockedAsync(admin.Id, true, "друг"));
        await Assert.ThrowsAsync<BusinessException>(() => svc.SetLockedAsync("няма", true, admin.Id));

        var page = await svc.ListAsync("klient", 1, 10);
        Assert.Equal(1, page.Total);
        Assert.Equal("klient@example.com", page.Items.Single().Email);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _sp.Dispose();
        _conn.Dispose();
    }
}
