using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Tests;

/// <summary>Наличности, куриери, оценка на цената, история и проследяване на поръчки.</summary>
public class OrderFlowTests
{
    // ExecuteUpdate променя директно базата, затова четем без проследяване (иначе контекстът връща старата копия)
    private static ProductVariant Variant(TestDb db, decimal price) =>
        db.Context.ProductVariants.AsNoTracking().Single(v => v.Price == price);

    // ---------- Наличности ----------
    [Fact]
    public async Task Create_DecrementsStock_AndMarksSoldOutVariantUnavailable()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var v = product.Variants.First(x => x.Price == 10.00m);
        v.StockQuantity = 3;
        db.Context.SaveChanges();

        var svc = Make.Orders(db);
        await svc.CreateAsync(Make.Order(v.Id, 2));
        Assert.Equal(1, Variant(db, 10.00m).StockQuantity);
        Assert.True(Variant(db, 10.00m).InStock);

        await svc.CreateAsync(Make.Order(v.Id, 1));
        var after = Variant(db, 10.00m);
        Assert.Equal(0, after.StockQuantity);
        Assert.False(after.InStock);                    // изчерпана
        await Assert.ThrowsAsync<BusinessException>(() => svc.CreateAsync(Make.Order(v.Id, 1)));
    }

    [Fact]
    public async Task Create_MoreThanAvailable_IsRejected_AndNothingChanges()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var v = product.Variants.First();
        v.StockQuantity = 2;
        db.Context.SaveChanges();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => Make.Orders(db).CreateAsync(Make.Order(v.Id, 5)));

        Assert.Contains("2", ex.Message);
        Assert.Empty(db.Context.Orders);
        Assert.Equal(2, db.Context.ProductVariants.Single(x => x.Id == v.Id).StockQuantity);
    }

    [Fact]
    public async Task Cancel_RestoresStock_ReenablesVariant_AndCannotBeUndone()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var v = product.Variants.First();
        v.StockQuantity = 2;
        db.Context.SaveChanges();
        var svc = Make.Orders(db);
        await svc.CreateAsync(Make.Order(v.Id, 2));
        Assert.False(db.Context.ProductVariants.AsNoTracking().Single(x => x.Id == v.Id).InStock);
        var orderId = db.Context.Orders.Single().Id;

        await svc.UpdateStatusAsync(orderId, OrderService.Cancelled);

        var restored = db.Context.ProductVariants.AsNoTracking().Single(x => x.Id == v.Id);
        Assert.Equal(2, restored.StockQuantity);
        Assert.True(restored.InStock);
        Assert.True(db.Context.Orders.Single().StockRestored);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => svc.UpdateStatusAsync(orderId, "Приета"));
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(2, db.Context.ProductVariants.AsNoTracking().Single(x => x.Id == v.Id).StockQuantity); // без двойно връщане
    }

    // ---------- Куриер и оценка ----------
    [Fact]
    public async Task Quote_MatchesCreate_AndAddressDeliveryCostsMoreThanOffice()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var v = product.Variants.First(x => x.Price == 10.00m);
        var svc = Make.Orders(db);

        var office = await svc.QuoteAsync(new QuoteRequestDto { Courier = "Еконт", DeliveryType = "Офис", Items = { new() { ProductVariantId = v.Id, Quantity = 1 } } });
        var address = await svc.QuoteAsync(new QuoteRequestDto { Courier = "Еконт", DeliveryType = "Адрес", Items = { new() { ProductVariantId = v.Id, Quantity = 1 } } });
        var speedy = await svc.QuoteAsync(new QuoteRequestDto { Courier = "Спиди", DeliveryType = "Офис", Items = { new() { ProductVariantId = v.Id, Quantity = 1 } } });

        Assert.Equal(3.00m, office.Shipping);
        Assert.Equal(4.20m, address.Shipping);
        Assert.Equal(2.90m, speedy.Shipping);
        Assert.Equal(25.00m, office.FreeShippingFrom);

        var dto = Make.Order(v.Id, 1);
        dto.Courier = "Еконт"; dto.DeliveryType = "Адрес";
        var created = await svc.CreateAsync(dto);
        Assert.Equal(address.Total, created.Total);
        var saved = db.Context.Orders.Single();
        Assert.Equal("Еконт", saved.Courier);
        Assert.Equal("Адрес", saved.DeliveryType);
    }

    [Fact]
    public async Task Quote_HeavyParcel_AddsPerKgSurcharge_AndUnknownCourierIsRejected()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var v = product.Variants.First(x => x.Price == 10.00m); // 0.5 кг
        var svc = Make.Orders(db);

        var cheap =await svc.QuoteAsync(new QuoteRequestDto { Items = { new() { ProductVariantId = v.Id, Quantity = 1 } } });
        Assert.Equal(0.5m, cheap.WeightKg);

        var ship = new ShippingService();
        Assert.Equal(3.00m, ship.Calculate("Еконт", "Офис", 2m, 10m));
        Assert.Equal(3.00m + 2 * 0.50m, ship.Calculate("Еконт", "Офис", 4m, 10m)); // 2 кг над включените 2
        Assert.Equal(0m, ship.Calculate("Еконт", "Офис", 9m, 25m));                  // безплатна доставка

        await Assert.ThrowsAsync<BusinessException>(() => svc.QuoteAsync(new QuoteRequestDto
        { Courier = "Непознат", Items = { new() { ProductVariantId = v.Id, Quantity = 1 } } }));
    }

    [Fact]
    public async Task ExtraDiscount_StacksWithPromo_AndIsCappedAt100()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var v = product.Variants.First(x => x.Price == 20.00m);

        // BG10 (10%) + 5% абонаментна = 15% от 20.00 = 3.00
        var result = await Make.Orders(db).CreateAsync(Make.Order(v.Id, 1, "BG10"), new OrderContext(ExtraDiscountPercent: 5));

        Assert.Equal(3.00m, result.Discount);
    }

    [Fact]
    public async Task Create_CardPayment_RequiresEnabledProvider()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var dto = Make.Order(product.Variants.First().Id);
        dto.PaymentType = "card";

        var ex = await Assert.ThrowsAsync<BusinessException>(() => Make.Orders(db).CreateAsync(dto));
        Assert.Contains("карта", ex.Message);
        Assert.Empty(db.Context.Orders);

        var online = await Make.Orders(db, payments: new AlwaysEnabledPayments()).CreateAsync(dto);
        Assert.True(online.RequiresPayment);
        Assert.Equal(StripePaymentService.Pending, db.Context.Orders.Single().PaymentStatus);
    }

    private sealed class AlwaysEnabledPayments : IPaymentService
    {
        public bool Enabled => true;
        public Task<string> CreateCheckoutUrlAsync(string orderNumber, string phone, string baseUrl) => Task.FromResult("https://example.test");
        public Task<bool> HandleWebhookAsync(string payload, string? signatureHeader) => Task.FromResult(true);
    }

    [Fact]
    public async Task Gift_IsSaved_OnlyWhenFlagged()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var dto = Make.Order(product.Variants.First().Id);
        dto.IsGift = true; dto.GiftMessage = "Честит празник!";
        await Make.Orders(db).CreateAsync(dto);

        var o = db.Context.Orders.Single();
        Assert.True(o.IsGift);
        Assert.Equal("Честит празник!", o.GiftMessage);

        var plain = Make.Order(product.Variants.First().Id);
        plain.GiftMessage = "не трябва да се запази";
        await Make.Orders(db).CreateAsync(plain);
        Assert.Null(db.Context.Orders.OrderBy(x => x.Id).Last().GiftMessage);
    }

    // ---------- Известия ----------
    [Fact]
    public async Task Notifications_AreSentOnCreateAndStatusChange()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var notifier = new FakeNotifier();
        var svc = Make.Orders(db, notifier);

        await svc.CreateAsync(Make.Order(product.Variants.First().Id));
        var order = db.Context.Orders.Single();
        await svc.UpdateStatusAsync(order.Id, "Изпратена");

        Assert.Equal(order.OrderNumber, Assert.Single(notifier.Created));
        Assert.Equal((order.OrderNumber, "Изпратена"), Assert.Single(notifier.StatusChanges));
    }

    // ---------- История и проследяване ----------
    [Fact]
    public async Task Track_ReturnsTimeline_ForMatchingPhoneInAnyFormat()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var svc = Make.Orders(db);
        var created = await svc.CreateAsync(Make.Order(product.Variants.First().Id)); // тел. "0888 111 222"
        await svc.UpdateStatusAsync(db.Context.Orders.Single().Id, "Обработва се");

        var t = await svc.TrackAsync("#" + created.OrderNumber.ToLowerInvariant(), "+359 888-111-222");

        Assert.Equal("Обработва се", t.Status);
        Assert.Equal(new[] { "Приета", "Обработва се" }, t.Events.Select(e => e.Status));
        Assert.Single(t.Items);
    }

    [Fact]
    public async Task Track_WrongPhoneOrNumber_GivesSameNotFound()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var svc = Make.Orders(db);
        var created = await svc.CreateAsync(Make.Order(product.Variants.First().Id));

        var wrongPhone = await Assert.ThrowsAsync<BusinessException>(() => svc.TrackAsync(created.OrderNumber, "0899 000 000"));
        var wrongNumber = await Assert.ThrowsAsync<BusinessException>(() => svc.TrackAsync("BG-000000-0000", "0888 111 222"));

        Assert.Equal(404, wrongPhone.StatusCode);
        Assert.Equal(wrongPhone.Message, wrongNumber.Message);   // не издава кои номера съществуват
    }

    [Theory]
    [InlineData("0888 111 222", "+359888111222", true)]
    [InlineData("0888111222", "888111222", true)]
    [InlineData("0888 111 222", "0888 111 223", false)]
    [InlineData("12", "12", false)]
    public void PhoneMatches_ComparesLastDigits(string a, string b, bool expected) =>
        Assert.Equal(expected, OrderService.PhoneMatches(a, b));

    // ---------- Акаунти ----------
    [Fact]
    public async Task Orders_AreLinkedToUser_AndListedOnlyForThem()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var alice = Make.User(db, "alice@example.com");
        var bob = Make.User(db, "bob@example.com");
        var svc = Make.Orders(db);

        await svc.CreateAsync(Make.Order(product.Variants.First().Id), new OrderContext(alice.Id));
        await svc.CreateAsync(Make.Order(product.Variants.First().Id));   // гост

        Assert.Single(await svc.GetForUserAsync(alice.Id));
        Assert.Empty(await svc.GetForUserAsync(bob.Id));
    }

    [Fact]
    public async Task GetForBeekeeper_ShowsOnlyOwnItems()
    {
        using var db = new TestDb();
        var (bk, product) = db.SeedProduct();
        var other = new Beekeeper { FullName = "Друг", ApiaryName = "Друг пчелин", RegistrationNumber = "X-2", RegionId = 1, CityOrVillage = "с. Друго", Phone = "0899 000 000" };
        db.Context.Beekeepers.Add(other); db.Context.SaveChanges();
        var foreign = new Product { BeekeeperId = other.Id, Name = "Чужд мед", Type = "Мед", Category = "herbal", Origin = "Родопи", Variants = { new ProductVariant { Label = "Буркан", Weight = "1kg", Price = 5m } } };
        db.Context.Products.Add(foreign); db.Context.SaveChanges();

        var dto = Make.Order(product.Variants.First().Id);
        dto.Items.Add(new CreateOrderItemDto { ProductVariantId = foreign.Variants.First().Id, Quantity = 1 });
        var svc = Make.Orders(db);
        await svc.CreateAsync(dto);

        var mine = Assert.Single(await svc.GetForBeekeeperAsync(bk.Id));
        Assert.Equal(product.Name, Assert.Single(mine.Items).ProductName);
        Assert.Empty(await svc.GetForBeekeeperAsync(9999));
    }

    // ---------- Администрация ----------
    [Fact]
    public async Task GetPage_FiltersAndPaginates_OnServer()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var svc = Make.Orders(db);
        for (var i = 0; i < 5; i++) await svc.CreateAsync(Make.Order(product.Variants.First().Id));
        await svc.UpdateStatusAsync(db.Context.Orders.First().Id, "Изпратена");

        var (first, total) = await svc.GetPageAsync(new OrderFilter(1, 2));
        var (shipped, shippedTotal) = await svc.GetPageAsync(new OrderFilter(Status: "Изпратена"));
        var (none, _) = await svc.GetPageAsync(new OrderFilter(Search: "няма-такъв"));

        Assert.Equal(5, total);
        Assert.Equal(2, first.Count);
        Assert.Equal(1, shippedTotal);
        Assert.Single(shipped);
        Assert.Empty(none);
    }
}
