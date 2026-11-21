using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Tests;

/// <summary>Отказ от клиента, разписка, скрити пчелини, управление на пчелари и региони, контакти, бюлетин, наличности, неплатени поръчки.</summary>
public class EngagementTests
{
    private static IConfiguration Config(params (string Key, string Value)[] items) =>
        new ConfigurationBuilder().AddInMemoryCollection(items.Select(i => new KeyValuePair<string, string?>(i.Key, i.Value))).Build();

    private static ProductVariant V(TestDb db, int id) => db.Context.ProductVariants.AsNoTracking().Single(v => v.Id == id);

    // ============================ Отказ на поръчка от клиента ============================
    [Fact]
    public async Task CustomerCancel_ByPhone_CancelsAndRestoresStock()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var v = product.Variants.First(); v.StockQuantity = 5; db.Context.SaveChanges();
        var svc = Make.Orders(db, new FakeNotifier());
        await svc.CreateAsync(Make.Order(v.Id, 2));
        var number = db.Context.Orders.Single().OrderNumber;
        Assert.Equal(3, V(db, v.Id).StockQuantity);

        var r = await svc.CancelByCustomerAsync(number, "0888111222", null, "Размислих");

        Assert.Equal(OrderService.Cancelled, r.Status);
        Assert.Equal(5, V(db, v.Id).StockQuantity);
        Assert.Contains(r.Events, e => e.Status == OrderService.Cancelled && e.Note!.Contains("Размислих"));
    }

    [Fact]
    public async Task CustomerCancel_WrongPhone_LooksLikeNotFound()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var svc = Make.Orders(db);
        await svc.CreateAsync(Make.Order(product.Variants.First().Id));
        var number = db.Context.Orders.Single().OrderNumber;

        var ex = await Assert.ThrowsAsync<BusinessException>(() => svc.CancelByCustomerAsync(number, "0999999999", null, null));
        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("Приета", db.Context.Orders.Single().Status);
    }

    [Fact]
    public async Task CustomerCancel_AccountOwner_NeedsNoPhone_ButOtherUserCannot()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var owner = Make.User(db, "owner@example.com"); var other = Make.User(db, "other@example.com");
        var svc = Make.Orders(db);
        await svc.CreateAsync(Make.Order(product.Variants.First().Id), new OrderContext(owner.Id));
        var number = db.Context.Orders.Single().OrderNumber;

        await Assert.ThrowsAsync<BusinessException>(() => svc.CancelByCustomerAsync(number, null, other.Id, null));
        var r = await svc.CancelByCustomerAsync(number, null, owner.Id, null);
        Assert.Equal(OrderService.Cancelled, r.Status);
    }

    [Theory]
    [InlineData("Обработва се")]
    [InlineData("Изпратена")]
    public async Task CustomerCancel_AfterProcessingStarted_IsRefused(string status)
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var svc = Make.Orders(db);
        await svc.CreateAsync(Make.Order(product.Variants.First().Id));
        var o = db.Context.Orders.Single();
        await svc.UpdateStatusAsync(o.Id, status);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => svc.CancelByCustomerAsync(o.OrderNumber, o.CustomerPhone, null, null));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CustomerCancel_PaidOnline_IsRefused_AndTwiceIsRefused()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var svc = Make.Orders(db);
        await svc.CreateAsync(Make.Order(product.Variants.First().Id));
        await svc.CreateAsync(Make.Order(product.Variants.Last().Id));
        var orders = db.Context.Orders.OrderBy(o => o.Id).ToList();
        orders[0].PaymentStatus = StripePaymentService.Paid; db.Context.SaveChanges();

        await Assert.ThrowsAsync<BusinessException>(() => svc.CancelByCustomerAsync(orders[0].OrderNumber, orders[0].CustomerPhone, null, null));
        await svc.CancelByCustomerAsync(orders[1].OrderNumber, orders[1].CustomerPhone, null, null);
        await Assert.ThrowsAsync<BusinessException>(() => svc.CancelByCustomerAsync(orders[1].OrderNumber, orders[1].CustomerPhone, null, null));
    }

    // ============================ Разписка ============================
    [Fact]
    public async Task Receipt_ContainsOrderData_AndEncodesHtml()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var svc = Make.Orders(db);
        var dto = Make.Order(product.Variants.First().Id);
        dto.CustomerName = "<script>alert(1)</script>";
        await svc.CreateAsync(dto);
        var number = db.Context.Orders.Single().OrderNumber;

        var order = await svc.FindForCustomerAsync(number, "0888 111 222", null);
        var html = OrderExport.ReceiptHtml(order, new ShopInfoDto("Фирма ЕООД", "123456789", "гр. София", "a@b.bg", "0888", ""));

        Assert.Contains(number, html);
        Assert.Contains("Фирма ЕООД", html);
        Assert.Contains("123456789", html);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        await Assert.ThrowsAsync<BusinessException>(() => svc.FindForCustomerAsync(number, "0111111111", null));
    }

    // ============================ Известие към пчеларя ============================
    [Fact]
    public async Task NewOrder_NotifiesBeekeeperWithEmail()
    {
        using var db = new TestDb();
        var (bk, product) = db.SeedProduct();
        bk.Email = "pchelar@example.com"; db.Context.SaveChanges();
        var notifier = new FakeNotifier();

        await Make.Orders(db, notifier).CreateAsync(Make.Order(product.Variants.First().Id));

        var n = Assert.Single(notifier.BeekeeperOrders);
        Assert.Equal("pchelar@example.com", n.Email);
        Assert.Equal("Тест Пчелин", n.Apiary);
    }

    [Fact]
    public async Task NewOrder_BeekeeperWithoutEmail_IsSkipped()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var notifier = new FakeNotifier();
        await Make.Orders(db, notifier).CreateAsync(Make.Order(product.Variants.First().Id));
        Assert.Empty(notifier.BeekeeperOrders);
    }

    // ============================ Скрити пчелини ============================
    [Fact]
    public async Task HiddenBeekeeper_DisappearsFromCatalog_AndCannotBeOrdered_UntilShownAgain()
    {
        using var db = new TestDb();
        var (bk, product) = db.SeedProduct();
        var beekeepers = new BeekeeperService(db.Context);
        var products = new ProductService(db.Context);

        await beekeepers.SetActiveAsync(bk.Id, false);

        Assert.Empty(await beekeepers.GetAllAsync(null));
        await Assert.ThrowsAsync<BusinessException>(() => beekeepers.GetByIdAsync(bk.Id));
        Assert.Equal(bk.Id, (await beekeepers.GetByIdAsync(bk.Id, includeHidden: true)).Id);
        Assert.Empty((await products.QueryAsync(new ProductQuery())).Items);
        Assert.Single((await products.QueryAsync(new ProductQuery(IncludeHidden: true))).Items);
        await Assert.ThrowsAsync<BusinessException>(() => Make.Orders(db).CreateAsync(Make.Order(product.Variants.First().Id)));

        await beekeepers.SetActiveAsync(bk.Id, true);
        Assert.Single(await beekeepers.GetAllAsync(null));
        Assert.Single((await products.QueryAsync(new ProductQuery())).Items);
        await Make.Orders(db).CreateAsync(Make.Order(product.Variants.First().Id));
    }

    // ============================ Администрация: пчелари ============================
    private static BeekeeperAdminUpsertDto Upsert(string region = "Родопи", string reg = "БАБХ-NEW-1") => new()
    {
        FullName = "Нов Пчелар", ApiaryName = "Нов Пчелин", RegistrationNumber = reg, Region = region, CityOrVillage = "с. Ново",
        Phone = "0888 555 555", Email = "nov@example.com", Bio = "Био", HiveCount = 30, ExperienceYears = 4, HasHoney = true, IsVerified = true, IsActive = true
    };

    [Fact]
    public async Task AdminUpdate_ChangesRegionAndFields_AndRejectsDuplicateRegistrationNumber()
    {
        using var db = new TestDb();
        var (bk, _) = db.SeedProduct();
        var svc = new BeekeeperService(db.Context);
        db.Context.Beekeepers.Add(new Beekeeper { FullName = "Друг", ApiaryName = "Друг пчелин", RegistrationNumber = "БАБХ-DUP", RegionId = 1, CityOrVillage = "х", Phone = "1" });
        db.Context.SaveChanges();

        var updated = await svc.AdminUpdateAsync(bk.Id, Upsert("Родопи"));
        Assert.Equal("Родопи", updated.Region);
        Assert.Equal("Нов Пчелин", updated.ApiaryName);
        Assert.Equal("nov@example.com", updated.Email);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => svc.AdminUpdateAsync(bk.Id, Upsert(reg: "БАБХ-DUP")));
        Assert.Equal(409, ex.StatusCode);
        await Assert.ThrowsAsync<BusinessException>(() => svc.AdminUpdateAsync(bk.Id, Upsert(region: "Несъществуващ")));
    }

    [Fact]
    public async Task AdminDelete_IsBlockedWhenOrdersExist_ButWorksOtherwise()
    {
        using var db = new TestDb();
        var (bk, product) = db.SeedProduct();
        var svc = new BeekeeperService(db.Context);
        await Make.Orders(db).CreateAsync(Make.Order(product.Variants.First().Id));

        var ex = await Assert.ThrowsAsync<BusinessException>(() => svc.DeleteAsync(bk.Id));
        Assert.Equal(409, ex.StatusCode);
        Assert.True(db.Context.Beekeepers.Any(b => b.Id == bk.Id));

        var empty = new Beekeeper { FullName = "Без поръчки", ApiaryName = "Празен", RegistrationNumber = "БАБХ-EMPTY", RegionId = 1, CityOrVillage = "х", Phone = "1" };
        db.Context.Beekeepers.Add(empty); db.Context.SaveChanges();
        await svc.DeleteAsync(empty.Id);
        Assert.False(db.Context.Beekeepers.Any(b => b.Id == empty.Id));
    }

    // ============================ Администрация: региони ============================
    [Fact]
    public async Task Regions_CreateUpdateDelete_WithGuards()
    {
        using var db = new TestDb();
        db.SeedProduct();
        var svc = new RegionService(db.Context);

        var created = await svc.CreateAsync(new RegionUpsertDto { Name = "Черноморие", Description = "Крайбрежие" });
        Assert.Equal("Черноморие", created.Name);
        await Assert.ThrowsAsync<BusinessException>(() => svc.CreateAsync(new RegionUpsertDto { Name = "Черноморие" }));
        await Assert.ThrowsAsync<BusinessException>(() => svc.UpdateAsync(created.Id, new RegionUpsertDto { Name = "Родопи" }));

        var renamed = await svc.UpdateAsync(created.Id, new RegionUpsertDto { Name = "Море", Description = "x" });
        Assert.Equal("Море", renamed.Name);

        await Assert.ThrowsAsync<BusinessException>(() => svc.DeleteAsync(1));   // „Странджа“ има пчелар
        await svc.DeleteAsync(created.Id);
        Assert.False(db.Context.Regions.Any(r => r.Id == created.Id));
    }

    // ============================ Контакти ============================
    [Fact]
    public async Task Contact_IsStored_AndForwardedToShopAndBeekeeper()
    {
        using var db = new TestDb();
        var (bk, _) = db.SeedProduct();
        bk.Email = "pchelar@example.com"; db.Context.SaveChanges();
        var notifier = new FakeNotifier();
        var svc = new ContactService(db.Context, Config(("Shop:ContactEmail", "shop@example.com")), notifier);

        await svc.SubmitAsync(new ContactDto { Name = "Иван", Email = "ivan@example.com", Subject = "Въпрос", Message = "Имате ли акациев мед?", BeekeeperId = bk.Id });

        var m = Assert.Single(db.Context.ContactMessages);
        Assert.Equal("Въпрос", m.Subject);
        var sent = Assert.Single(notifier.Contacts);
        Assert.Equal("shop@example.com", sent.Shop);
        Assert.Equal("pchelar@example.com", sent.Beekeeper);

        var list = await svc.GetAllAsync();
        Assert.Equal("Тест Пчелин", list.Single().ApiaryName);
        Assert.True((await svc.SetHandledAsync(m.Id, true)).IsHandled);
        await svc.DeleteAsync(m.Id);
        Assert.Empty(db.Context.ContactMessages);
    }

    [Fact]
    public async Task Contact_ToUnknownOrHiddenBeekeeper_IsRejected()
    {
        using var db = new TestDb();
        var (bk, _) = db.SeedProduct();
        var svc = new ContactService(db.Context, Config(), new FakeNotifier());
        var dto = new ContactDto { Name = "И", Email = "i@example.com", Subject = "т", Message = "Достатъчно дълго съобщение", BeekeeperId = 9999 };
        await Assert.ThrowsAsync<BusinessException>(() => svc.SubmitAsync(dto));

        bk.IsActive = false; db.Context.SaveChanges();
        dto.BeekeeperId = bk.Id;
        await Assert.ThrowsAsync<BusinessException>(() => svc.SubmitAsync(dto));
        Assert.Empty(db.Context.ContactMessages);
    }

    // ============================ Бюлетин ============================
    [Fact]
    public async Task Newsletter_SubscribeIsIdempotent_UnsubscribeByToken_SendsOnlyToActive()
    {
        using var db = new TestDb();
        var notifier = new FakeNotifier();
        var svc = new NewsletterService(db.Context, notifier);

        await svc.SubscribeAsync("Ana@Example.com");
        await svc.SubscribeAsync("ana@example.com");     // същият имейл, друга главна/малка
        await svc.SubscribeAsync("boris@example.com");
        Assert.Equal(2, db.Context.NewsletterSubscribers.Count());

        var ana = db.Context.NewsletterSubscribers.Single(s => s.Email == "ana@example.com");
        Assert.True(await svc.UnsubscribeAsync(ana.Token));
        Assert.False(await svc.UnsubscribeAsync("няма-такъв"));

        var sent = await svc.SendAsync(new NewsletterSendDto { Subject = "Нов мед", Body = "Здравейте\n\nНова реколта" }, "https://example.com");
        Assert.Equal(1, sent);
        Assert.Equal(("boris@example.com", "Нов мед"), Assert.Single(notifier.Newsletters));

        await svc.SubscribeAsync("ana@example.com");     // повторно записване
        Assert.True(db.Context.NewsletterSubscribers.AsNoTracking().Single(s => s.Email == "ana@example.com").IsActive);
    }

    [Fact]
    public async Task Newsletter_SendWithoutSubscribers_Fails()
    {
        using var db = new TestDb();
        await Assert.ThrowsAsync<BusinessException>(() => new NewsletterService(db.Context, new FakeNotifier())
            .SendAsync(new NewsletterSendDto { Subject = "т", Body = "т" }, "https://example.com"));
    }

    // ============================ „Уведоми ме“ ============================
    [Fact]
    public async Task StockAlert_OnlyForUnavailable_AndNotifiedOnceWhenBackInStock()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var notifier = new FakeNotifier();
        var alerts = new StockAlertService(db.Context, Config(("App:PublicUrl", "https://example.com")), notifier);
        var products = new ProductService(db.Context, alerts);
        var variant = product.Variants.First();

        // наличен продукт – известие няма смисъл
        await Assert.ThrowsAsync<BusinessException>(() => alerts.SubscribeAsync(product.Id, "k@example.com"));

        foreach (var v in product.Variants) await products.SetStockAsync(v.Id, 0, null);
        await alerts.SubscribeAsync(product.Id, "k@example.com");
        await alerts.SubscribeAsync(product.Id, "K@example.com");   // дубликат
        Assert.Single(db.Context.StockAlerts);
        Assert.Empty(notifier.StockAvailables);

        await products.SetStockAsync(variant.Id, 7, null);
        Assert.Equal(("k@example.com", "Тестов мед"), Assert.Single(notifier.StockAvailables));
        Assert.Empty(db.Context.StockAlerts);                       // изпратено веднъж и изтрито
    }

    [Fact]
    public async Task StockAlert_AlsoTriggeredWhenProductIsMarkedInStockAgain()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct(inStock: false);
        var notifier = new FakeNotifier();
        var alerts = new StockAlertService(db.Context, Config(), notifier);
        await alerts.SubscribeAsync(product.Id, "k@example.com");

        await new ProductService(db.Context, alerts).SetStockAsync(product.Variants.First().Id, 5, null);
        Assert.Empty(notifier.StockAvailables);                     // продуктът още е скрит (InStock = false)
        Assert.Single(db.Context.StockAlerts);
    }

    // ============================ Неплатени онлайн поръчки ============================
    [Fact]
    public async Task StalePendingPayments_AreCancelled_AndStockReturned_RecentOnesKept()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var v = product.Variants.First(); v.StockQuantity = 10; db.Context.SaveChanges();
        var svc = Make.Orders(db, new FakeNotifier());
        await svc.CreateAsync(Make.Order(v.Id, 2));
        await svc.CreateAsync(Make.Order(v.Id, 3));
        await svc.CreateAsync(Make.Order(v.Id, 1));     // наложен платеж – не се пипа
        var orders = db.Context.Orders.OrderBy(o => o.Id).ToList();
        orders[0].PaymentStatus = StripePaymentService.Pending; orders[0].CreatedAt = DateTime.UtcNow.AddHours(-30);
        orders[1].PaymentStatus = StripePaymentService.Pending; orders[1].CreatedAt = DateTime.UtcNow.AddHours(-2);
        orders[2].CreatedAt = DateTime.UtcNow.AddHours(-30);
        db.Context.SaveChanges();
        Assert.Equal(4, V(db, v.Id).StockQuantity);

        var n = await svc.CancelStalePendingPaymentsAsync(TimeSpan.FromHours(24));

        Assert.Equal(1, n);
        var fresh = db.Context.Orders.AsNoTracking().OrderBy(o => o.Id).ToList();
        Assert.Equal(OrderService.Cancelled, fresh[0].Status);
        Assert.Equal("Приета", fresh[1].Status);
        Assert.Equal("Приета", fresh[2].Status);
        Assert.Equal(6, V(db, v.Id).StockQuantity);     // върнати са бройките на първата поръчка
    }
}
