using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Tests;

/// <summary>Адреси, любими и абонаменти на клиентите.</summary>
public class AccountAndSubscriptionTests
{
    private static AddressUpsertDto Addr(string label = "Дом", bool isDefault = false) => new()
    {
        Label = label, FullName = "Иван Иванов", Phone = "0888 123 456", City = "София", Address = "ул. Тест 1", IsDefault = isDefault
    };

    // ---------- Адреси ----------
    [Fact]
    public async Task FirstAddress_BecomesDefault_AndSettingAnotherDefaultMovesTheFlag()
    {
        using var db = new TestDb();
        var u = Make.User(db);
        var svc = new AccountService(db.Context);

        var a = await svc.AddAddressAsync(u.Id, Addr("Дом"));
        var b = await svc.AddAddressAsync(u.Id, Addr("Работа", isDefault: true));

        Assert.True(a.IsDefault);
        var all = await svc.GetAddressesAsync(u.Id);
        Assert.Equal("Работа", all.Single(x => x.IsDefault).Label);
        Assert.Equal(2, all.Count);

        await svc.DeleteAddressAsync(u.Id, b.Id);   // изтрит е основният -> другият става основен
        Assert.True(Assert.Single(await svc.GetAddressesAsync(u.Id)).IsDefault);
    }

    [Fact]
    public async Task Addresses_AreIsolatedPerUser_AndLimited()
    {
        using var db = new TestDb();
        var alice = Make.User(db, "a@example.com"); var bob = Make.User(db, "b@example.com");
        var svc = new AccountService(db.Context);
        var a = await svc.AddAddressAsync(alice.Id, Addr());

        Assert.Empty(await svc.GetAddressesAsync(bob.Id));
        await Assert.ThrowsAsync<BusinessException>(() => svc.UpdateAddressAsync(bob.Id, a.Id, Addr("Чужд")));
        await Assert.ThrowsAsync<BusinessException>(() => svc.DeleteAddressAsync(bob.Id, a.Id));

        for (var i = 1; i < AccountService.MaxAddresses; i++) await svc.AddAddressAsync(alice.Id, Addr("A" + i));
        await Assert.ThrowsAsync<BusinessException>(() => svc.AddAddressAsync(alice.Id, Addr("над лимита")));
    }

    // ---------- Любими ----------
    [Fact]
    public async Task Wishlist_AddIsIdempotent_RemoveWorks_AndUnknownProductIsRejected()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var u = Make.User(db);
        var svc = new AccountService(db.Context);

        await svc.AddToWishlistAsync(u.Id, product.Id);
        await svc.AddToWishlistAsync(u.Id, product.Id);
        Assert.Single(await svc.GetWishlistAsync(u.Id));

        await Assert.ThrowsAsync<BusinessException>(() => svc.AddToWishlistAsync(u.Id, 9999));

        await svc.RemoveFromWishlistAsync(u.Id, product.Id);
        Assert.Empty(await svc.GetWishlistIdsAsync(u.Id));
    }

    [Fact]
    public async Task Wishlist_MergeIgnoresUnknownAndDuplicateIds()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var u = Make.User(db);
        var svc = new AccountService(db.Context);
        await svc.AddToWishlistAsync(u.Id, product.Id);

        await svc.MergeWishlistAsync(u.Id, new[] { product.Id, product.Id, 9999, 8888 });

        Assert.Equal(new[] { product.Id }, await svc.GetWishlistIdsAsync(u.Id));
    }

    // ---------- Абонаменти ----------
    private static SubscriptionService Subs(TestDb db, FakeNotifier? notifier = null) =>
        new(db.Context, Make.Orders(db, notifier), new ShippingService());

    private static SubscriptionCreateDto NewSub(int variantId, DateTime? first = null) => new()
    {
        ProductVariantId = variantId, Quantity = 2, IntervalMonths = 1, CustomerName = "Абонат", CustomerPhone = "0888 555 666",
        CustomerEmail = "abonat@example.com", DeliveryCity = "Пловдив", DeliveryAddress = "Офис Еконт Център",
        FirstDeliveryDate = first ?? DateTime.UtcNow.AddDays(2)
    };

    [Fact]
    public async Task CreateSubscription_ValidatesDateVariantAndGiftSets()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var u = Make.User(db);
        var svc = Subs(db);
        var variantId = product.Variants.First().Id;

        var ok = await svc.CreateAsync(u.Id, NewSub(variantId));
        Assert.Equal(Subscription.Active, ok.Status);
        Assert.Equal(SubscriptionService.DiscountPercent, ok.DiscountPercent);

        await Assert.ThrowsAsync<BusinessException>(() => svc.CreateAsync(u.Id, NewSub(variantId, DateTime.UtcNow.AddDays(-1))));
        await Assert.ThrowsAsync<BusinessException>(() => svc.CreateAsync(u.Id, NewSub(variantId, DateTime.UtcNow.AddDays(200))));
        await Assert.ThrowsAsync<BusinessException>(() => svc.CreateAsync(u.Id, NewSub(9999)));

        product.Type = "Подаръчен комплект"; db.Context.SaveChanges();
        await Assert.ThrowsAsync<BusinessException>(() => svc.CreateAsync(u.Id, NewSub(variantId)));
    }

    [Fact]
    public async Task ProcessDue_CreatesDiscountedOrder_AdvancesDate_AndDecrementsStock()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var u = Make.User(db);
        var notifier = new FakeNotifier();
        var svc = Subs(db, notifier);
        var variant = product.Variants.First(v => v.Price == 20.00m);
        var created = await svc.CreateAsync(u.Id, NewSub(variant.Id, DateTime.UtcNow.AddDays(1)));

        // още не е настъпил денят
        Assert.Equal(0, await svc.ProcessDueAsync(DateTime.UtcNow));

        var due = DateTime.UtcNow.AddDays(2);
        Assert.Equal(1, await svc.ProcessDueAsync(due));

        var order = db.Context.Orders.Include(o => o.Items).Single();
        Assert.Equal(u.Id, order.UserId);
        Assert.Equal(created.Id, order.SubscriptionId);
        Assert.Equal(2, order.Items.Single().Quantity);
        Assert.Equal(2.00m, order.DiscountAmount);                 // 5% от 40.00 €
        Assert.Equal("Наложен платеж", order.PaymentStatus);
        Assert.Equal(98, db.Context.ProductVariants.AsNoTracking().Single(v => v.Id == variant.Id).StockQuantity);
        Assert.Single(notifier.Created);

        var sub = db.Context.Subscriptions.AsNoTracking().Single();
        Assert.Equal(1, sub.OrdersCreated);
        Assert.True(sub.NextDeliveryDate > due);

        // повторно изпълнение в същия ден не създава дубликат
        Assert.Equal(0, await svc.ProcessDueAsync(due));
        Assert.Single(db.Context.Orders);
    }

    [Fact]
    public async Task ProcessDue_SkipsPausedAndCancelled_AndPostponesWhenOutOfStock()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var u = Make.User(db);
        var svc = Subs(db);
        var variant = product.Variants.First();

        var paused = await svc.CreateAsync(u.Id, NewSub(variant.Id, DateTime.UtcNow.AddDays(1)));
        await svc.SetStatusAsync(u.Id, paused.Id, "pause");
        Assert.Equal(0, await svc.ProcessDueAsync(DateTime.UtcNow.AddDays(5)));

        await svc.SetStatusAsync(u.Id, paused.Id, "resume");
        variant.StockQuantity = 0; variant.InStock = false; db.Context.SaveChanges();
        var now = DateTime.UtcNow.AddDays(5);
        Assert.Equal(0, await svc.ProcessDueAsync(now));            // няма наличност – отлага се
        Assert.Empty(db.Context.Orders);
        Assert.Equal(now.Date.AddDays(3), db.Context.Subscriptions.AsNoTracking().Single().NextDeliveryDate);

        await svc.SetStatusAsync(u.Id, paused.Id, "cancel");
        Assert.Empty(await svc.GetForUserAsync(u.Id));              // прекратените не се показват
        await Assert.ThrowsAsync<BusinessException>(() => svc.SetStatusAsync(u.Id, paused.Id, "resume"));
    }

    [Fact]
    public async Task Subscriptions_CannotBeManagedByAnotherUser_AndAreLimited()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var alice = Make.User(db, "a@example.com"); var bob = Make.User(db, "b@example.com");
        var svc = Subs(db);
        var s = await svc.CreateAsync(alice.Id, NewSub(product.Variants.First().Id));

        await Assert.ThrowsAsync<BusinessException>(() => svc.SetStatusAsync(bob.Id, s.Id, "cancel"));

        for (var i = 1; i < SubscriptionService.MaxActivePerUser; i++) await svc.CreateAsync(alice.Id, NewSub(product.Variants.First().Id));
        await Assert.ThrowsAsync<BusinessException>(() => svc.CreateAsync(alice.Id, NewSub(product.Variants.First().Id)));
    }
}
