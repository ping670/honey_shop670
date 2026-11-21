using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Data;

/// <summary>
/// Контекст на базата данни (Microsoft SQL Server). Наследява IdentityDbContext, така че таблиците
/// на ASP.NET Core Identity (AspNetUsers, AspNetRoles…) са в същата база като данните на платформата.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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
    public DbSet<OrderEvent> OrderEvents => Set<OrderEvent>();
    public DbSet<SavedAddress> SavedAddresses => Set<SavedAddress>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<StockAlert> StockAlerts => Set<StockAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Identity таблиците

        modelBuilder.Entity<ContactMessage>(e =>
        {
            e.HasIndex(m => m.CreatedAt);
            e.HasOne(m => m.Beekeeper).WithMany().HasForeignKey(m => m.BeekeeperId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<NewsletterSubscriber>(e =>
        {
            e.HasIndex(s => s.Email).IsUnique();
            e.HasIndex(s => s.Token).IsUnique();
        });

        modelBuilder.Entity<StockAlert>(e =>
        {
            e.HasIndex(a => new { a.ProductId, a.Email }).IsUnique();
            e.HasOne(a => a.Product).WithMany().HasForeignKey(a => a.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderEvent>(e =>
        {
            e.HasIndex(x => x.OrderId);
            e.HasOne(x => x.Order).WithMany(o => o.Events).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SavedAddress>(e => e.HasIndex(a => a.UserId));

        modelBuilder.Entity<WishlistItem>(e =>
        {
            e.HasKey(w => new { w.UserId, w.ProductId });
            e.HasOne(w => w.Product).WithMany().HasForeignKey(w => w.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.HasIndex(s => s.UserId);
            e.HasIndex(s => new { s.Status, s.NextDeliveryDate });
            e.HasOne(s => s.ProductVariant).WithMany().HasForeignKey(s => s.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Article>(e =>
        {
            e.HasIndex(a => a.Slug).IsUnique();
            e.HasIndex(a => new { a.IsPublished, a.PublishedAt });
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.At);
            e.HasIndex(a => a.UserName);
        });

        modelBuilder.Entity<Region>(e =>
        {
            e.HasIndex(r => r.Name).IsUnique();
            e.HasData(Region.Seed);
        });

        modelBuilder.Entity<Beekeeper>(e =>
        {
            e.HasIndex(b => b.RegistrationNumber).IsUnique();
            e.HasIndex(b => b.UserId).IsUnique();   // един акаунт – един пчелин (NULL-овете не се броят)
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.SetNull);
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
            e.HasIndex(o => o.UserId);
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
