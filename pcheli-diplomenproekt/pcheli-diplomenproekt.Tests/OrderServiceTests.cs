using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Tests;

public class OrderServiceTests
{
    private static CreateOrderDto NewOrder(int variantId, int qty, string? promo = null) => new()
    {
        CustomerName = "Клиент", CustomerPhone = "0888 111 222", DeliveryCity = "София", DeliveryAddress = "ул. Тест 1",
        PromoCode = promo,
        Items = { new CreateOrderItemDto { ProductVariantId = variantId, Quantity = qty } }
    };

    private static OrderService CreateService(TestDb db) => new(db.Context, new PromoService(db.Context));

    [Fact]
    public async Task Create_AppliesPromoCode_AndCalculatesTotalsOnServer()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct(); // BG10 (10%) идва от HasData
        var variant = product.Variants.First(v => v.Price == 20.00m);

        // 1 × 20.00 € = 20.00 €; -10% = 18.00 €; под 25 € => доставка 3.00 €; общо 21.00 €
        var result = await CreateService(db).CreateAsync(NewOrder(variant.Id, 1, "bg10"));

        Assert.Equal(20.00m, result.Subtotal);
        Assert.Equal(2.00m, result.Discount);
        Assert.Equal(3.00m, result.Shipping);
        Assert.Equal(21.00m, result.Total);
        Assert.StartsWith("BG-", result.OrderNumber);

        var saved = db.Context.Orders.Single();
        Assert.Equal(result.OrderNumber, saved.OrderNumber);
        Assert.Equal(variant.Id, db.Context.OrderItems.Single().ProductVariantId);
    }

    [Fact]
    public async Task Create_FreeShipping_WhenNetTotalReachesThreshold()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var variant = product.Variants.First(v => v.Price == 20.00m);

        var result = await CreateService(db).CreateAsync(NewOrder(variant.Id, 3)); // 60.00 €

        Assert.Equal(0m, result.Shipping);
        Assert.Equal(60.00m, result.Total);
    }

    [Fact]
    public async Task Create_NonExistingVariant_IsRejected_AndNothingIsSaved()
    {
        using var db = new TestDb();
        db.SeedProduct();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => CreateService(db).CreateAsync(NewOrder(9999, 1)));

        Assert.Equal(400, ex.StatusCode);
        Assert.Empty(db.Context.Orders);
    }

    [Fact]
    public async Task Create_OutOfStockProduct_IsRejected()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct(inStock: false);

        await Assert.ThrowsAsync<BusinessException>(() => CreateService(db).CreateAsync(NewOrder(product.Variants.First().Id, 1)));
        Assert.Empty(db.Context.Orders);
    }

    [Fact]
    public async Task Create_IgnoresClientPrices_AndMergesDuplicateLines()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var variant = product.Variants.First(v => v.Price == 10.00m);
        var dto = NewOrder(variant.Id, 1);
        dto.Items.Add(new CreateOrderItemDto { ProductVariantId = variant.Id, Quantity = 1 });

        var result = await CreateService(db).CreateAsync(dto);

        var item = Assert.Single(db.Context.OrderItems);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(product.Id, item.ProductId);        // OrderItem.ProductId се попълва
        Assert.Equal(20.00m, result.Subtotal);           // цената е от базата
        Assert.Equal(23.00m, result.Total);              // + 3.00 € доставка
    }

    [Fact]
    public async Task Create_QuantityOverLimit_IsRejected()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var id = product.Variants.First().Id;
        var dto = NewOrder(id, 30);
        dto.Items.Add(new CreateOrderItemDto { ProductVariantId = id, Quantity = 30 });

        await Assert.ThrowsAsync<BusinessException>(() => CreateService(db).CreateAsync(dto));
        Assert.Empty(db.Context.Orders);
    }

    [Fact]
    public async Task Create_InvalidPromoCode_IsRejected()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateService(db).CreateAsync(NewOrder(product.Variants.First().Id, 1, "NOPE")));

        Assert.Equal(404, ex.StatusCode);
        Assert.Empty(db.Context.Orders);
    }
}
