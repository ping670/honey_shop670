using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Tests;

/// <summary>Търсене, филтри, сортиране, странициране, наличности и произход на продуктите.</summary>
public class CatalogFeatureTests
{
    private static async Task<(TestDb Db, ProductService Svc)> SeededAsync()
    {
        var db = new TestDb();
        await DbInitializer.SeedCatalogAsync(db.Context);
        return (db, new ProductService(db.Context));
    }

    [Fact]
    public async Task Sort_ByPrice_UsesLowestVariantPrice()
    {
        var (db, svc) = await SeededAsync();
        using var _ = db;

        var asc = (await svc.QueryAsync(new ProductQuery(Sort: "price-asc"))).Items;
        var desc = (await svc.QueryAsync(new ProductQuery(Sort: "price-desc"))).Items;

        decimal Min(ProductDto p) => p.Variants.Min(v => v.Price);
        Assert.Equal(asc.Select(Min).OrderBy(x => x), asc.Select(Min));
        Assert.Equal(desc.Select(Min).OrderByDescending(x => x), desc.Select(Min));
        Assert.Equal(asc.First().Id, desc.Last().Id);
    }

    [Fact]
    public async Task PriceRange_MatchesProductsWithAnyVariantInsideRange()
    {
        var (db, svc) = await SeededAsync();
        using var _ = db;

        var cheap = (await svc.QueryAsync(new ProductQuery(MaxPrice: 6m))).Items;
        var expensive = (await svc.QueryAsync(new ProductQuery(MinPrice: 20m))).Items;

        Assert.NotEmpty(cheap);
        Assert.All(cheap, p => Assert.Contains(p.Variants, v => v.Price <= 6m));
        Assert.All(expensive, p => Assert.Contains(p.Variants, v => v.Price >= 20m));
        Assert.Equal("Пресен Суров Замразен Пчелен Прашец", Assert.Single(expensive).Name);   // 21 €
    }

    [Fact]
    public async Task Pagination_ReturnsPageAndTotalCount()
    {
        var (db, svc) = await SeededAsync();
        using var _ = db;

        var p1 = await svc.QueryAsync(new ProductQuery(Page: 1, PageSize: 4));
        var p3 = await svc.QueryAsync(new ProductQuery(Page: 3, PageSize: 4));
        var all = await svc.QueryAsync(new ProductQuery());

        Assert.Equal(9, p1.Total);
        Assert.Equal(4, p1.Items.Count);
        Assert.Single(p3.Items);                    // 9 = 4 + 4 + 1
        Assert.Equal(9, all.Items.Count);           // без Page – всички
        Assert.Empty(p1.Items.Select(p => p.Id).Intersect(p3.Items.Select(p => p.Id)));
    }

    [Fact]
    public async Task InStockOnly_HidesSoldOutProducts_AndBeekeeperFilterWorks()
    {
        var (db, svc) = await SeededAsync();
        using var _ = db;
        var first = db.Context.Products.OrderBy(p => p.Id).First();
        foreach (var v in db.Context.ProductVariants.Where(v => v.ProductId == first.Id)) { v.StockQuantity = 0; v.InStock = false; }
        db.Context.SaveChanges();

        var inStock = (await svc.QueryAsync(new ProductQuery(InStockOnly: true))).Items;
        var byBeekeeper = (await svc.QueryAsync(new ProductQuery(BeekeeperId: first.BeekeeperId))).Items;

        Assert.DoesNotContain(inStock, p => p.Id == first.Id);
        Assert.Equal(8, inStock.Count);
        Assert.All(byBeekeeper, p => Assert.Equal(first.BeekeeperId, p.Beekeeper!.Id));
    }

    [Fact]
    public async Task Search_FindsByEnglishName()
    {
        var (db, svc) = await SeededAsync();
        using var _ = db;
        await SeedExtras.EnrichProductsAsync(db.Context);

        var found = (await svc.QueryAsync(new ProductQuery(Search: "Lavender"))).Items;

        Assert.Equal("Добруджански Лавандулов Мед", Assert.Single(found).Name);
    }

    [Fact]
    public async Task Enrich_AddsProvenanceAndEnglish_Idempotently_AndGiftSetsAreSeededOnce()
    {
        var (db, _) = await SeededAsync();
        using var __ = db;

        await SeedExtras.EnrichProductsAsync(db.Context);
        await SeedExtras.EnrichProductsAsync(db.Context);
        await SeedExtras.SeedGiftSetsAsync(db.Context);
        await SeedExtras.SeedGiftSetsAsync(db.Context);

        var manov = db.Context.Products.Single(p => p.Category == "manov");
        Assert.Equal("STR-2026-08", manov.BatchNumber);
        Assert.Equal(2026, manov.HarvestYear);
        Assert.Contains("ЗНП", manov.Certificates);
        Assert.Contains("БАБХ-4001-0812", manov.Certificates);
        Assert.Equal(2, db.Context.Products.Count(p => p.Type == "Подаръчен комплект"));
        Assert.Equal(11, db.Context.Products.Count());
    }

    [Fact]
    public async Task Batch_LookupFindsProduct_AndEmptyBatchFindsNothing()
    {
        var (db, svc) = await SeededAsync();
        using var _ = db;
        await SeedExtras.EnrichProductsAsync(db.Context);

        Assert.Single(await svc.GetByBatchAsync("STR-2026-08"));
        Assert.Empty(await svc.GetByBatchAsync("NOPE"));
        Assert.Empty(await svc.GetByBatchAsync("  "));
    }

    // ---------- Управление на продукти ----------
    private static ProductUpsertDto NewProduct(int beekeeperId, string type = "Мед") => new()
    {
        BeekeeperId = beekeeperId, Name = "Нов мед", Type = type, Category = "herbal", Origin = "Родопи",
        Variants = { new VariantUpsertDto { Label = "Буркан", Weight = "0.5kg", Price = 7m, StockQuantity = 25 } }
    };

    [Fact]
    public async Task Create_StoresProvenanceStockAndRejectsBadTypeOrUnsafeUrls()
    {
        using var db = new TestDb();
        var (bk, _) = db.SeedProduct();
        var svc = new ProductService(db.Context);

        var dto = NewProduct(bk.Id);
        dto.BatchNumber = "B-1"; dto.HarvestYear = 2026; dto.LabSummary = "Влага 17%"; dto.NameEn = "New honey";
        var created = await svc.CreateAsync(dto);
        Assert.Equal("B-1", created.BatchNumber);
        Assert.Equal(25, created.Variants.Single().Stock);

        await Assert.ThrowsAsync<BusinessException>(() => svc.CreateAsync(NewProduct(bk.Id, "Оръжие")));

        var evil = NewProduct(bk.Id); evil.LabReportUrl = "javascript:alert(1)";
        await Assert.ThrowsAsync<BusinessException>(() => svc.CreateAsync(evil));
        var evilImg = NewProduct(bk.Id); evilImg.Image = "//evil.example/x.png";
        await Assert.ThrowsAsync<BusinessException>(() => svc.CreateAsync(evilImg));
    }

    [Fact]
    public async Task Inventory_ListsLowStock_AndSetStockTogglesAvailability()
    {
        using var db = new TestDb();
        var (bk, product) = db.SeedProduct();
        var svc = new ProductService(db.Context);
        var variant = product.Variants.First();
        variant.StockQuantity = 4; db.Context.SaveChanges();

        var low = await svc.GetInventoryAsync(null, lowOnly: true);
        Assert.Contains(low, i => i.VariantId == variant.Id && i.IsLow);

        var zero = await svc.SetStockAsync(variant.Id, 0, null);
        Assert.False(zero.InStock);
        var back = await svc.SetStockAsync(variant.Id, 50, bk.Id);
        Assert.True(back.InStock);
        Assert.False(back.IsLow);
    }

    [Fact]
    public async Task SetStock_ByAnotherBeekeeper_IsForbidden()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var svc = new ProductService(db.Context);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => svc.SetStockAsync(product.Variants.First().Id, 5, beekeeperId: 12345));

        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [InlineData("0.9kg", 0.9)]
    [InlineData("250g", 0.25)]
    [InlineData("1,5 kg", 1.5)]
    [InlineData("", 0.5)]
    [InlineData("много", 0.5)]
    public void ParseWeightKg_HandlesCommonFormats(string weight, double expected) =>
        Assert.Equal((decimal)expected, ProductVariant.ParseWeightKg(weight));
}
