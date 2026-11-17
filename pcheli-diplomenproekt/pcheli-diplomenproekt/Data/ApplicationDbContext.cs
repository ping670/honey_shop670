using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Data;

/// <summary>
/// Контекст на базата данни (Microsoft SQL Server). Наследява IdentityDbContext, така че таблиците
/// на ASP.NET Core Identity (AspNetUsers, AspNetRoles…) са в същата база като данните на платформата.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Beekeeper> Beekeepers => Set<Beekeeper>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<BeekeeperApplication> BeekeeperApplications => Set<BeekeeperApplication>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Identity таблиците

        modelBuilder.Entity<Region>(e =>
        {
            e.HasIndex(r => r.Name).IsUnique();
            e.HasData(Region.Seed);
        });

        modelBuilder.Entity<Beekeeper>(e =>
        {
            e.HasIndex(b => b.RegistrationNumber).IsUnique();
            e.HasOne(b => b.Region)
                .WithMany(r => r.Beekeepers)
                .HasForeignKey(b => b.RegionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(b => b.Products)
                .WithOne(p => p.Beekeeper)
                .HasForeignKey(p => p.BeekeeperId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BeekeeperApplication>(e =>
        {
            e.HasIndex(a => a.Status);
            e.HasIndex(a => a.RegistrationNumber);
            e.HasOne(a => a.Region).WithMany().HasForeignKey(a => a.RegionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Beekeeper).WithMany().HasForeignKey(a => a.BeekeeperId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(p => p.Type);
            e.HasIndex(p => p.Category);
            e.HasMany(p => p.Variants)
                .WithOne(v => v.Product)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.HasIndex(o => o.CreatedAt);
            e.HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Историята на поръчките не трябва да изчезва при изтриване на продукт
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.ProductVariant).WithMany().HasForeignKey(i => i.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PromoCode>(e =>
        {
            e.HasIndex(p => p.Code).IsUnique();
            e.HasData(new PromoCode { Id = 1, Code = "BG10", DiscountPercent = 10, IsActive = true, ValidUntil = null });
        });

        modelBuilder.Entity<Review>(e =>
        {
            e.HasIndex(r => new { r.IsApproved, r.CreatedAt });
            e.HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.SetNull);
            e.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5"));
        });
    }
}
