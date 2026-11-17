using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

public interface IOrderService
{
    Task<OrderResultDto> CreateAsync(CreateOrderDto dto);
    Task<List<OrderDto>> GetRecentAsync(int take = 200);
    Task<OrderDto> UpdateStatusAsync(int id, string status);
    Task<StatsDto> GetStatsAsync();
}

public class OrderService : IOrderService
{
    public const decimal FreeShippingThreshold = 25.00m; // €
    public const decimal ShippingCost = 3.00m;           // €
    public static readonly string[] Statuses = { "Приета", "Обработва се", "Изпратена", "Доставена", "Отказана" };

    private readonly ApplicationDbContext _db;
    private readonly IPromoService _promos;
    public OrderService(ApplicationDbContext db, IPromoService promos) { _db = db; _promos = promos; }

    public async Task<OrderResultDto> CreateAsync(CreateOrderDto dto)
    {
        // Обединяваме дублирани редове за една и съща опаковка
        var lines = dto.Items.GroupBy(i => i.ProductVariantId)
            .Select(g => (VariantId: g.Key, Qty: g.Sum(x => x.Quantity))).ToList();
        if (lines.Any(l => l.Qty > 50))
            throw new BusinessException("Максималното количество за един продукт е 50.");

        var ids = lines.Select(l => l.VariantId).ToList();
        var variants = await _db.ProductVariants
            .Include(v => v.Product).ThenInclude(p => p!.Beekeeper)
            .Where(v => ids.Contains(v.Id)).ToListAsync();

        if (variants.Count != ids.Count)
            throw new BusinessException("Някои от продуктите в количката вече не съществуват.");

        var order = new Order
        {
            CustomerName = dto.CustomerName.Trim(),
            CustomerPhone = dto.CustomerPhone.Trim(),
            CustomerEmail = dto.CustomerEmail?.Trim() ?? string.Empty,
            DeliveryCity = dto.DeliveryCity.Trim(),
            DeliveryAddress = dto.DeliveryAddress.Trim(),
            Notes = dto.Notes,
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Наложен платеж (при доставка)" : dto.PaymentMethod,
            Status = "Приета",
            CreatedAt = DateTime.UtcNow
        };

        foreach (var (variantId, qty) in lines)
        {
            var v = variants.First(x => x.Id == variantId);
            if (!v.InStock || v.Product is null || !v.Product.InStock)
                throw new BusinessException($"„{v.Product?.Name}“ в момента не е налично.");

            order.Items.Add(new OrderItem
            {
                ProductId = v.ProductId,
                ProductVariantId = v.Id,
                ProductName = v.Product.Name,
                ApiaryName = v.Product.Beekeeper?.ApiaryName ?? string.Empty,
                Weight = v.Label,
                Quantity = qty,
                UnitPrice = v.Price,
                TotalPrice = v.Price * qty
            });
        }

        order.Subtotal = order.Items.Sum(i => i.TotalPrice);

        if (!string.IsNullOrWhiteSpace(dto.PromoCode))
        {
            var promo = await _promos.ValidateAsync(dto.PromoCode); // хвърля 404, ако е невалиден
            order.PromoCode = promo.Code;
            order.DiscountAmount = Math.Round(order.Subtotal * promo.DiscountPercent / 100m, 2);
        }

        var net = order.Subtotal - order.DiscountAmount;
        order.ShippingAmount = net >= FreeShippingThreshold ? 0m : ShippingCost;
        order.TotalAmount = net + order.ShippingAmount;

        // Запис в транзакция. Номерът BG-yyMMdd-XXXX се проверява за колизия; ако междувременно
        // друга заявка вземе същия номер, уникалният индекс хвърля грешка и опитваме с нов номер.
        for (var attempt = 0; ; attempt++)
        {
            order.OrderNumber = await GenerateOrderNumberAsync();
            try
            {
                await using var tx = await _db.Database.BeginTransactionAsync();
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                break;
            }
            catch (DbUpdateException) when (attempt < 4)
            {
                _db.ChangeTracker.Clear();
                // Повтаряме само при колизия на номера; всяка друга грешка се хвърля нагоре
                if (!await _db.Orders.AsNoTracking().AnyAsync(o => o.OrderNumber == order.OrderNumber))
                    throw;
                // Reattach-ваме поръчката като нова
                foreach (var item in order.Items) { item.Id = 0; item.OrderId = 0; item.Order = null; item.Product = null; item.ProductVariant = null; }
                order.Id = 0;
            }
        }

        return new OrderResultDto(order.OrderNumber, order.Subtotal, order.DiscountAmount, order.ShippingAmount,
            order.TotalAmount, "Поръчката Ви е приета успешно! Пчеларите бяха уведомени и ще се свържат с Вас за доставка.");
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        string number;
        do number = $"BG-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 10000)}";
        while (await _db.Orders.AnyAsync(o => o.OrderNumber == number));
        return number;
    }

    public async Task<List<OrderDto>> GetRecentAsync(int take = 200)
    {
        var orders = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt).Take(take).ToListAsync();
        return orders.Select(o => o.ToDto()).ToList();
    }

    public async Task<OrderDto> UpdateStatusAsync(int id, string status)
    {
        if (!Statuses.Contains(status))
            throw new BusinessException("Невалиден статус. Позволени: " + string.Join(", ", Statuses));
        var o = await _db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException("Поръчката не е намерена.", StatusCodes.Status404NotFound);
        o.Status = status;
        await _db.SaveChangesAsync();
        return o.ToDto();
    }

    public async Task<StatsDto> GetStatsAsync() => new(
        await _db.Beekeepers.CountAsync(),
        await _db.Products.CountAsync(),
        await _db.Products.CountAsync(p => p.Type == "Мед"),
        await _db.Products.CountAsync(p => p.Type == "Пчелен прашец"),
        await _db.Orders.CountAsync(),
        await _db.Beekeepers.Select(b => b.RegionId).Distinct().CountAsync(),
        await _db.Beekeepers.SumAsync(b => b.HiveCount));
}
