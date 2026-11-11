using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Beekeeper> Beekeepers => Set<Beekeeper>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<BeekeeperApplication> BeekeeperApplications => Set<BeekeeperApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // One Beekeeper to Many Products
        modelBuilder.Entity<Beekeeper>()
            .HasMany(b => b.Products)
            .WithOne(p => p.Beekeeper)
            .HasForeignKey(p => p.BeekeeperId)
            .OnDelete(DeleteBehavior.Cascade);

        // One Order to Many OrderItems
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
