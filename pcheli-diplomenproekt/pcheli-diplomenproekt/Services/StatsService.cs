using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;

namespace pcheli_diplomenproekt.Services;

public interface IStatsService
{
    Task<DashboardDto> GetDashboardAsync(DateTime? nowUtc = null);
}

/// <summary>Показатели за таблото на администратора. Отказаните поръчки не се броят към приходите.</summary>
public class StatsService : IStatsService
{
    private readonly ApplicationDbContext _db;
    public StatsService(ApplicationDbContext db) => _db = db;

    public async Task<DashboardDto> GetDashboardAsync(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var cancelled = OrderService.Cancelled;

        // Сумите се сумират в паметта – така заявките работят еднакво на SQL Server и SQLite (в тестовете)
        var orders = await _db.Orders.AsNoTracking().Where(o => o.Status != cancelled)
            .Select(o => new { o.CreatedAt, o.TotalAmount }).ToListAsync();

        var since30 = now.AddDays(-30);
        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var revenue30 = orders.Where(o => o.CreatedAt >= since30).Sum(o => o.TotalAmount);
        var orders30 = orders.Count(o => o.CreatedAt >= since30);

        // последните 12 месеца, включително месеци без поръчки
        var firstMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
        var monthly = Enumerable.Range(0, 12).Select(i =>
        {
            var m = firstMonth.AddMonths(i);
            var inMonth = orders.Where(o => o.CreatedAt.Year == m.Year && o.CreatedAt.Month == m.Month).ToList();
            return new MonthlyRevenueDto($"{m:yyyy-MM}", inMonth.Sum(o => o.TotalAmount), inMonth.Count);
        }).ToList();

        var byStatus = await _db.Orders.AsNoTracking().GroupBy(o => o.Status)
            .Select(g => new StatusCountDto(g.Key, g.Count())).ToListAsync();
        byStatus = OrderService.Statuses.Select(s => byStatus.FirstOrDefault(x => x.Status == s) ?? new StatusCountDto(s, 0)).ToList();

        var items = await _db.OrderItems.AsNoTracking().Where(i => i.Order!.Status != cancelled)
            .Select(i => new { i.ProductId, i.ProductName, i.ApiaryName, i.Quantity, i.TotalPrice }).ToListAsync();

        var topProducts = items.GroupBy(i => i.ProductId)
            .Select(g => new TopProductDto(g.Key, g.First().ProductName, g.Sum(x => x.Quantity), g.Sum(x => x.TotalPrice)))
            .OrderByDescending(p => p.Revenue).Take(5).ToList();
        var topBeekeepers = items.Where(i => i.ApiaryName.Length > 0).GroupBy(i => i.ApiaryName)
            .Select(g => new TopBeekeeperDto(g.Key, g.Sum(x => x.Quantity), g.Sum(x => x.TotalPrice)))
            .OrderByDescending(b => b.Revenue).Take(5).ToList();

        return new DashboardDto(
            totalRevenue, revenue30, orders.Count, orders30,
            PendingOrders: await _db.Orders.CountAsync(o => o.Status == "Приета" || o.Status == "Обработва се"),
            AverageOrderValue: orders.Count == 0 ? 0m : Math.Round(totalRevenue / orders.Count, 2),
            LowStockCount: await _db.ProductVariants.CountAsync(v => v.StockQuantity <= ProductService.LowStockThreshold),
            ActiveSubscriptions: await _db.Subscriptions.CountAsync(s => s.Status == Models.Subscription.Active),
            PendingApplications: await _db.BeekeeperApplications.CountAsync(a => a.Status == BeekeeperService.Pending),
            PendingReviews: await _db.Reviews.CountAsync(r => !r.IsApproved),
            CustomersCount: await _db.Users.CountAsync(),
            monthly, byStatus, topProducts, topBeekeepers);
    }
}
