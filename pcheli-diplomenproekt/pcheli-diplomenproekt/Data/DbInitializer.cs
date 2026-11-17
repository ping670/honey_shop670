using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Data;

public static class DbInitializer
{
    public const string AdminRole = "Admin";

    /// <summary>
    /// Прилага EF Core миграциите и (идемпотентно) попълва началните данни и администратора.
    /// Регионите и промо кодът BG10 се създават от самата миграция (HasData).
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        await context.Database.MigrateAsync();

        await SeedCatalogAsync(context);
        await SeedReviewsAsync(context);
        await SeedAdminAsync(services, logger);
    }

    public static async Task SeedCatalogAsync(ApplicationDbContext context)
    {
        if (await context.Beekeepers.AnyAsync()) return;

        await using var tx = await context.Database.BeginTransactionAsync();
        var beekeepers = SeedData.Beekeepers();
        context.Beekeepers.AddRange(beekeepers);
        await context.SaveChangesAsync();

        context.Products.AddRange(SeedData.Products(beekeepers));
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static async Task SeedReviewsAsync(ApplicationDbContext context)
    {
        if (await context.Reviews.AnyAsync()) return;

        context.Reviews.AddRange(
            new Review
            {
                AuthorName = "Петър Николов", City = "гр. София", Rating = 5, IsApproved = true,
                CreatedAt = new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc),
                Comment = "Поръчах странджански манов мед и суров замразен прашец. Мановият мед е плътен и истински, без никаква следа от претопляне. Прашецът дойде перфектно запазен. Чудесно е, че можем да поръчваме директно от пчеларите."
            },
            new Review
            {
                AuthorName = "Десислава Тодорова", City = "гр. Пловдив", Rating = 5, IsApproved = true,
                CreatedAt = new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc),
                Comment = "Акациевият мед от Тракия е любим на децата ми — много светъл и нежен. Пчеларят Стоян се обади да потвърди адреса и го донесе лично на следващия ден. Препоръчвам горещо!"
            },
            new Review
            {
                AuthorName = "Красимир Иванов", City = "гр. Варна", Rating = 5, IsApproved = true,
                CreatedAt = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                Comment = "Взех липов мед и лавандулов от Добруджа. Качеството е отлично, вкусът е точно какъвто го помня от детството ми. Браво за платформата и подкрепата за българските производители!"
            });
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Създава ролята Admin и един администратор. Потребителското име и паролата се четат от конфигурацията
    /// (User Secrets в Development или променливи на средата Admin__Username / Admin__Password) – никога от кода.
    /// </summary>
    private static async Task SeedAdminAsync(IServiceProvider services, ILogger logger)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        if (!await roles.RoleExistsAsync(AdminRole))
            await roles.CreateAsync(new IdentityRole(AdminRole));

        var username = config["Admin:Username"];
        var password = config["Admin:Password"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Admin:Username / Admin:Password не са конфигурирани – администратор не е създаден. " +
                              "Задайте ги с 'dotnet user-secrets set'.");
            return;
        }

        var user = await users.FindByNameAsync(username);
        if (user is null)
        {
            user = new IdentityUser { UserName = username, Email = config["Admin:Email"], EmailConfirmed = true };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError("Администраторът не беше създаден: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }
            logger.LogInformation("Създаден е администратор '{User}'.", username);
        }

        if (!await users.IsInRoleAsync(user, AdminRole))
            await users.AddToRoleAsync(user, AdminRole);
    }
}
