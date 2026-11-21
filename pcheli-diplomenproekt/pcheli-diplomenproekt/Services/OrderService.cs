using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Services;

/// <summary>Допълнителен контекст при създаване на поръчка (влязъл клиент, абонамент, допълнителна отстъпка).</summary>
public record OrderContext(string? UserId = null, int? SubscriptionId = null, int ExtraDiscountPercent = 0);

public record OrderFilter(int Page = 1, int PageSize = 25, string? Status = null, string? Search = null,
    DateTime? From = null, DateTime? To = null);

public interface IOrderService
{
    Task<OrderResultDto> CreateAsync(CreateOrderDto dto, OrderContext? context = null);
    Task<QuoteDto> QuoteAsync(QuoteRequestDto dto, int extraDiscountPercent = 0);
    Task<List<OrderDto>> GetRecentAsync(int take = 200);
    Task<(List<OrderDto> Items, int Total)> GetPageAsync(OrderFilter filter);
    Task<List<OrderDto>> GetByIdsAsync(IEnumerable<int> ids);
    Task<List<OrderDto>> GetForUserAsync(string userId);
    Task<List<OrderDto>> GetForBeekeeperAsync(int beekeeperId, int take = 200);
    Task<TrackingDto> TrackAsync(string orderNumber, string phone);
    Task<OrderDto> UpdateStatusAsync(int id, string status, string? note = null);
    Task<OrderDto> UpdateStatusForBeekeeperAsync(int beekeeperId, int orderId, string status);
    /// <summary>Клиентът отказва поръчката си, докато е само „Приета“ и не е платена онлайн. Достъп: собственикът на акаунта или номер + телефон.</summary>
    Task<TrackingDto> CancelByCustomerAsync(string orderNumber, string? phone, string? userId, string? reason);
    /// <summary>Намира поръчка за клиент (собственик на акаунта или номер + телефон) – за разписка.</summary>
    Task<OrderDto> FindForCustomerAsync(string orderNumber, string? phone, string? userId);
    /// <summary>Отказва поръчки, чакащи онлайн плащане повече от зададеното време, и връща наличностите. Връща броя.</summary>
    Task<int> CancelStalePendingPaymentsAsync(TimeSpan olderThan);
    Task<StatsDto> GetStatsAsync();
}

public class OrderService : IOrderService
{
    public const decimal FreeShippingThreshold = 25.00m; // € (по подразбиране; конфигурира се в „Shipping:FreeShippingFrom“)
    public const decimal ShippingCost = 3.00m;           // € (Еконт до офис, до 2 кг)
    public const int MaxQuantity = 50;
    public const string Cancelled = "Отказана";
    public static readonly string[] Statuses = { "Приета", "Обработва се", "Изпратена", "Доставена", Cancelled };

    private readonly ApplicationDbContext _db;
    private readonly IPromoService _promos;
    private readonly IShippingService _shipping;
    private readonly INotificationService? _notify;
    private readonly IPaymentService? _payments;

    public OrderService(ApplicationDbContext db, IPromoService promos, IShippingService? shipping = null,
        INotificationService? notify = null, IPaymentService? payments = null)
    {
        _db = db; _promos = promos; _shipping = shipping ?? new ShippingService(); _notify = notify; _payments = payments;
    }

    // ---------- Изчисляване на цената (общо за оценка и за реална поръчка) ----------
    private sealed record PricedLine(ProductVariant Variant, int Qty);
    private sealed record Priced(List<PricedLine> Lines, decimal Subtotal, decimal Discount, int DiscountPercent,
        string? PromoCode, decimal Shipping, decimal WeightKg, string Courier, string DeliveryType)
    {
        public decimal Net => Subtotal - Discount;
        public decimal Total => Net + Shipping;
    }

    private async Task<Priced> PriceAsync(IEnumerable<CreateOrderItemDto> items, string? promoCode, string? courierName,
        string? deliveryTypeName, int extraDiscountPercent)
    {
        // Обединяваме дублирани редове за една и съща опаковка
        var merged = items.GroupBy(i => i.ProductVariantId)
            .Select(g => (VariantId: g.Key, Qty: g.Sum(x => x.Quantity))).ToList();
        if (merged.Count == 0) throw new BusinessException("Количката е празна.");
        if (merged.Any(l => l.Qty > MaxQuantity))
            throw new BusinessException($"Максималното количество за един продукт е {MaxQuantity}.");

        var ids = merged.Select(l => l.VariantId).ToList();
        var variants = await _db.ProductVariants
            .Include(v => v.Product).ThenInclude(p => p!.Beekeeper)
            .Where(v => ids.Contains(v.Id)).ToListAsync();

        if (variants.Count != ids.Count)
            throw new BusinessException("Някои от продуктите в количката вече не съществуват.");

        var lines = new List<PricedLine>();
        foreach (var (variantId, qty) in merged)
        {
            var v = variants.First(x => x.Id == variantId);
            if (!v.InStock || v.Product is null || !v.Product.InStock || v.StockQuantity <= 0 || v.Product.Beekeeper is { IsActive: false })
                throw new BusinessException($"„{v.Product?.Name}“ в момента не е налично.");
            if (v.StockQuantity < qty)
                throw new BusinessException($"„{v.Product.Name}“ ({v.Label}): налични са само {v.StockQuantity} бр.");
            lines.Add(new PricedLine(v, qty));
        }

        var subtotal = lines.Sum(l => l.Variant.Price * l.Qty);

        var percent = 0;
        string? code = null;
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            var promo = await _promos.ValidateAsync(promoCode); // хвърля 404, ако е невалиден
            code = promo.Code;
            percent = promo.DiscountPercent;
        }
        percent = Math.Min(100, percent + Math.Max(0, extraDiscountPercent));
        var discount = Math.Round(subtotal * percent / 100m, 2);

        var courier = _shipping.NormalizeCourier(courierName);
        var type = _shipping.NormalizeDeliveryType(deliveryTypeName);
        var weight = lines.Sum(l => l.Variant.WeightKg * l.Qty);
        var shipping = _shipping.Calculate(courier, type, weight, subtotal - discount);

        return new Priced(lines, subtotal, discount, percent, code, shipping, weight, courier, type);
    }

    public async Task<QuoteDto> QuoteAsync(QuoteRequestDto dto, int extraDiscountPercent = 0)
    {
        var p = await PriceAsync(dto.Items, dto.PromoCode, dto.Courier, dto.DeliveryType, extraDiscountPercent);
        return new QuoteDto(p.Subtotal, p.Discount, p.Shipping, p.Total, _shipping.FreeShippingFrom, p.WeightKg,
            p.Courier, p.DeliveryType, p.PromoCode, p.DiscountPercent);
    }

    // ---------- Създаване ----------
    public async Task<OrderResultDto> CreateAsync(CreateOrderDto dto, OrderContext? context = null)
    {
        context ??= new OrderContext();
        var priced = await PriceAsync(dto.Items, dto.PromoCode, dto.Courier, dto.DeliveryType, context.ExtraDiscountPercent);

        var online = string.Equals(dto.PaymentType, "card", StringComparison.OrdinalIgnoreCase);
        if (online && _payments is not { Enabled: true })
            throw new BusinessException("Плащането с карта в момента не е налично. Изберете наложен платеж.");

        var paymentMethod = online ? "Банкова карта (онлайн)"
            : string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Наложен платеж (при доставка)" : dto.PaymentMethod.Trim();
        if (paymentMethod.Length > 50) paymentMethod = paymentMethod[..50];

        var order = new Order
        {
            UserId = context.UserId,
            SubscriptionId = context.SubscriptionId,
            CustomerName = dto.CustomerName.Trim(),
            CustomerPhone = dto.CustomerPhone.Trim(),
            CustomerEmail = dto.CustomerEmail?.Trim() ?? string.Empty,
            DeliveryCity = dto.DeliveryCity.Trim(),
            DeliveryAddress = dto.DeliveryAddress.Trim(),
            Notes = dto.Notes,
            PaymentMethod = paymentMethod,
            PaymentStatus = online ? StripePaymentService.Pending : "Наложен платеж",
            Courier = priced.Courier,
            DeliveryType = priced.DeliveryType,
            IsGift = dto.IsGift,
            GiftMessage = dto.IsGift ? dto.GiftMessage?.Trim() : null,
            Status = "Приета",
            CreatedAt = DateTime.UtcNow,
            Subtotal = priced.Subtotal,
            PromoCode = priced.PromoCode,
            DiscountAmount = priced.Discount,
            ShippingAmount = priced.Shipping,
            TotalAmount = priced.Total
        };
        order.Events.Add(new OrderEvent { Status = "Приета", At = order.CreatedAt });

        foreach (var l in priced.Lines)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = l.Variant.ProductId,
                ProductVariantId = l.Variant.Id,
                ProductName = l.Variant.Product!.Name,
                ApiaryName = l.Variant.Product.Beekeeper?.ApiaryName ?? string.Empty,
                Weight = l.Variant.Label,
                Quantity = l.Qty,
                UnitPrice = l.Variant.Price,
                TotalPrice = l.Variant.Price * l.Qty
            });
        }

        // Запис в транзакция: поръчката + атомично намаляване на наличностите. Номерът BG-yyMMdd-XXXX се проверява
        // за колизия; ако междувременно друга заявка вземе същия номер, уникалният индекс хвърля грешка и опитваме с нов.
        for (var attempt = 0; ; attempt++)
        {
            order.OrderNumber = await GenerateOrderNumberAsync();
            try
            {
                await using var tx = await _db.Database.BeginTransactionAsync();
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                foreach (var l in priced.Lines)
                {
                    var variantId = l.Variant.Id; var qty = l.Qty;
                    // условието StockQuantity >= qty прави намаляването безопасно при паралелни поръчки
                    var updated = await _db.ProductVariants.Where(v => v.Id == variantId && v.StockQuantity >= qty)
                        .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity - qty));
                    if (updated == 0)
                        throw new BusinessException($"„{l.Variant.Product!.Name}“ ({l.Variant.Label}) току-що свърши. Обновете количката.");
                }
                var lineIds = priced.Lines.Select(l => l.Variant.Id).ToList();
                await _db.ProductVariants.Where(v => lineIds.Contains(v.Id) && v.StockQuantity <= 0 && v.InStock)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.InStock, false));

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
                foreach (var ev in order.Events) { ev.Id = 0; ev.OrderId = 0; ev.Order = null; }
                order.Id = 0;
            }
        }

        _notify?.OrderCreated(order);
        NotifyBeekeepers(order, priced.Lines);

        return new OrderResultDto(order.OrderNumber, order.Subtotal, order.DiscountAmount, order.ShippingAmount,
            order.TotalAmount,
            online ? "Поръчката е записана. Остава да платите онлайн."
                   : "Поръчката Ви е приета успешно! Пчеларите бяха уведомени и ще се свържат с Вас за доставка.",
            online, order.PaymentStatus);
    }

    /// <summary>Всеки пчелар с имейл получава писмо само с неговите артикули от поръчката.</summary>
    private void NotifyBeekeepers(Order order, List<PricedLine> lines)
    {
        if (_notify is null) return;
        foreach (var g in lines.Where(l => !string.IsNullOrWhiteSpace(l.Variant.Product?.Beekeeper?.Email))
                     .GroupBy(l => l.Variant.Product!.Beekeeper!.Id))
        {
            var bk = g.First().Variant.Product!.Beekeeper!;
            var variantIds = g.Select(l => l.Variant.Id).ToHashSet();
            var items = order.Items.Where(i => variantIds.Contains(i.ProductVariantId)).ToList();
            try { _notify.BeekeeperNewOrder(bk.Email, bk.ApiaryName, order, items); }
            catch { /* известието е допълнително – не бива да проваля поръчката */ }
        }
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        string number;
        do number = $"BG-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 10000)}";
        while (await _db.Orders.AnyAsync(o => o.OrderNumber == number));
        return number;
    }

    // ---------- Четене ----------
    private IQueryable<Order> ReadQuery() => _db.Orders.AsNoTracking().Include(o => o.Items).Include(o => o.Events);

    public async Task<List<OrderDto>> GetRecentAsync(int take = 200) =>
        (await ReadQuery().OrderByDescending(o => o.CreatedAt).Take(take).ToListAsync()).Select(o => o.ToDto()).ToList();

    public async Task<(List<OrderDto> Items, int Total)> GetPageAsync(OrderFilter f)
    {
        var q = ReadQuery();
        if (!string.IsNullOrWhiteSpace(f.Status)) q = q.Where(o => o.Status == f.Status);
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim();
            q = q.Where(o => o.OrderNumber.Contains(s) || o.CustomerName.Contains(s) || o.CustomerPhone.Contains(s) || o.CustomerEmail.Contains(s));
        }
        if (f.From.HasValue) q = q.Where(o => o.CreatedAt >= f.From.Value);
        if (f.To.HasValue) { var to = f.To.Value.Date.AddDays(1); q = q.Where(o => o.CreatedAt < to); }

        var page = Math.Max(1, f.Page); var size = Math.Clamp(f.PageSize, 1, 5000);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync();
        return (items.Select(o => o.ToDto()).ToList(), total);
    }

    public async Task<List<OrderDto>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var list = ids.Distinct().Take(200).ToList();
        return (await ReadQuery().Where(o => list.Contains(o.Id)).OrderBy(o => o.Id).ToListAsync()).Select(o => o.ToDto()).ToList();
    }

    public async Task<List<OrderDto>> GetForUserAsync(string userId) =>
        (await ReadQuery().Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).Take(100).ToListAsync())
        .Select(o => o.ToDto()).ToList();

    public async Task<List<OrderDto>> GetForBeekeeperAsync(int beekeeperId, int take = 200)
    {
        var productIds = await _db.Products.Where(p => p.BeekeeperId == beekeeperId).Select(p => p.Id).ToListAsync();
        var orders = await ReadQuery().Where(o => o.Items.Any(i => productIds.Contains(i.ProductId)))
            .OrderByDescending(o => o.CreatedAt).Take(take).ToListAsync();
        // Пчеларят вижда само собствените си артикули в поръчката
        return orders.Select(o =>
        {
            var dto = o.ToDto();
            var mine = dto.Items.Where(i => productIds.Contains(i.ProductId)).ToList();
            // Статусът е на цялата поръчка – пчеларят го сменя само когато всички артикули са негови
            var sole = mine.Count == dto.Items.Count && o.Status != Cancelled && o.Status != "Доставена";
            return dto with { Items = mine, Subtotal = mine.Sum(i => i.TotalPrice), CanUpdateStatus = sole };
        }).ToList();
    }

    /// <summary>Пчеларят може само да придвижва поръчка напред (Обработва се → Изпратена → Доставена), и то ако всички артикули са негови.</summary>
    public async Task<OrderDto> UpdateStatusForBeekeeperAsync(int beekeeperId, int orderId, string status)
    {
        string[] allowed = { "Обработва се", "Изпратена", "Доставена" };
        if (!allowed.Contains(status))
            throw new BusinessException("Можете да променяте статуса само на „Обработва се“, „Изпратена“ или „Доставена“.");

        var o = await _db.Orders.AsNoTracking().Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == orderId)
                ?? throw new BusinessException("Поръчката не е намерена.", StatusCodes.Status404NotFound);
        var productIds = await _db.Products.Where(p => p.BeekeeperId == beekeeperId).Select(p => p.Id).ToListAsync();
        if (!o.Items.Any(i => productIds.Contains(i.ProductId)))
            throw new BusinessException("Поръчката не е намерена.", StatusCodes.Status404NotFound);
        if (!o.Items.All(i => productIds.Contains(i.ProductId)))
            throw new BusinessException("Поръчката съдържа продукти от няколко пчелина – статусът се променя от администратор.", StatusCodes.Status403Forbidden);
        if (Array.IndexOf(Statuses, status) <= Array.IndexOf(Statuses, o.Status))
            throw new BusinessException("Статусът може да се променя само напред.", StatusCodes.Status409Conflict);

        return await UpdateStatusAsync(orderId, status);
    }

    // ---------- Проследяване ----------
    public static bool PhoneMatches(string? a, string? b)
    {
        static string Digits(string? s) => new((s ?? string.Empty).Where(char.IsDigit).ToArray());
        var da = Digits(a); var db = Digits(b);
        if (da.Length < 6 || db.Length < 6) return false;
        var n = Math.Min(9, Math.Min(da.Length, db.Length));
        return da[^n..] == db[^n..];
    }

    public async Task<TrackingDto> TrackAsync(string orderNumber, string phone)
    {
        var number = (orderNumber ?? string.Empty).Trim().TrimStart('#').ToUpperInvariant();
        var o = await _db.Orders.AsNoTracking().Include(x => x.Items).Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.OrderNumber == number);
        // един и същ отговор при грешен номер и при грешен телефон – не издаваме кои номера съществуват
        if (o is null || !PhoneMatches(o.CustomerPhone, phone))
            throw new BusinessException("Не намерихме поръчка с този номер и телефон.", StatusCodes.Status404NotFound);

        var dto = o.ToDto();
        return new TrackingDto(o.OrderNumber, o.Status, o.PaymentStatus, o.PaymentMethod, o.CreatedAt, o.Courier,
            o.DeliveryType, o.DeliveryCity, o.TotalAmount, o.IsGift, dto.Items,
            o.Events.OrderBy(e => e.At).ThenBy(e => e.Id).Select(e => new OrderEventDto(e.Status, e.Note, e.At)).ToList());
    }

    // ---------- Статус ----------
    // ---------- Отказ от клиента, разписка, изтекли плащания ----------
    private async Task<Order> FindOwnedAsync(string orderNumber, string? phone, string? userId, bool track)
    {
        var number = (orderNumber ?? string.Empty).Trim().TrimStart('#').ToUpperInvariant();
        var q = _db.Orders.Include(x => x.Items).Include(x => x.Events).AsQueryable();
        if (!track) q = q.AsNoTracking();
        var o = await q.FirstOrDefaultAsync(x => x.OrderNumber == number);
        var owner = o != null && userId != null && o.UserId == userId;
        // еднакъв отговор при грешен номер и при грешен телефон
        if (o is null || !(owner || PhoneMatches(o.CustomerPhone, phone)))
            throw new BusinessException("Не намерихме поръчка с този номер и телефон.", StatusCodes.Status404NotFound);
        return o;
    }

    public async Task<OrderDto> FindForCustomerAsync(string orderNumber, string? phone, string? userId) =>
        (await FindOwnedAsync(orderNumber, phone, userId, track: false)).ToDto();

    public async Task<TrackingDto> CancelByCustomerAsync(string orderNumber, string? phone, string? userId, string? reason)
    {
        var o = await FindOwnedAsync(orderNumber, phone, userId, track: true);
        if (o.Status == Cancelled) throw new BusinessException("Поръчката вече е отказана.", StatusCodes.Status409Conflict);
        if (o.Status != "Приета")
            throw new BusinessException("Поръчката вече се обработва или е изпратена. Свържете се с нас, за да уговорим отказ или връщане.", StatusCodes.Status409Conflict);
        if (o.PaymentStatus == StripePaymentService.Paid)
            throw new BusinessException("Поръчката е платена онлайн. Свържете се с нас – ще я откажем и ще възстановим сумата.", StatusCodes.Status409Conflict);

        var note = string.IsNullOrWhiteSpace(reason) ? "Отказана от клиента" : "Отказана от клиента: " + reason.Trim();
        await UpdateStatusAsync(o.Id, Cancelled, note.Length > 200 ? note[..200] : note);
        return await TrackAsync(o.OrderNumber, o.CustomerPhone);
    }

    public async Task<int> CancelStalePendingPaymentsAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var ids = await _db.Orders.AsNoTracking()
            .Where(o => o.Status == "Приета" && o.CreatedAt < cutoff &&
                        (o.PaymentStatus == StripePaymentService.Pending || o.PaymentStatus == StripePaymentService.Failed))
            .Select(o => o.Id).ToListAsync();
        foreach (var id in ids)
            await UpdateStatusAsync(id, Cancelled, "Автоматично отказана – не е получено плащане навреме");
        return ids.Count;
    }

    public async Task<OrderDto> UpdateStatusAsync(int id, string status, string? note = null)
    {
        if (!Statuses.Contains(status))
            throw new BusinessException("Невалиден статус. Позволени: " + string.Join(", ", Statuses));
        var o = await _db.Orders.Include(x => x.Items).Include(x => x.Events).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new BusinessException("Поръчката не е намерена.", StatusCodes.Status404NotFound);
        if (o.Status == status) return o.ToDto();
        if (o.Status == Cancelled)
            throw new BusinessException("Отказана поръчка не може да бъде възстановена. Създайте нова.", StatusCodes.Status409Conflict);

        await using var tx = await _db.Database.BeginTransactionAsync();
        o.Status = status;
        o.Events.Add(new OrderEvent { Status = status, Note = note, At = DateTime.UtcNow });

        if (status == Cancelled && !o.StockRestored)
        {
            // Върнатите бройки правят опаковката отново налична (само ако е била изчерпана)
            foreach (var item in o.Items)
            {
                var variantId = item.ProductVariantId; var qty = item.Quantity;
                await _db.ProductVariants.Where(v => v.Id == variantId && v.StockQuantity <= 0)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.InStock, true));
                await _db.ProductVariants.Where(v => v.Id == variantId)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity + qty));
            }
            o.StockRestored = true;
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        _notify?.OrderStatusChanged(o);
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
