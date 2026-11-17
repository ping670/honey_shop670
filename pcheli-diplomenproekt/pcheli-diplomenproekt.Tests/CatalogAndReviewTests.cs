using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Tests;

public class CatalogAndReviewTests
{
    [Fact]
    public async Task Seed_CreatesFullCatalog_AndIsIdempotent()
    {
        using var db = new TestDb();

        await DbInitializer.SeedCatalogAsync(db.Context);
        await DbInitializer.SeedCatalogAsync(db.Context); // втори път не трябва да дублира

        Assert.Equal(6, db.Context.Beekeepers.Count());
        Assert.Equal(9, db.Context.Products.Count());
        Assert.Equal(7, db.Context.Regions.Count());                     // HasData
        Assert.Equal("BG10", db.Context.PromoCodes.Single().Code);       // HasData
    }

    [Fact]
    public async Task Products_FilterByTypeRegionCategoryAndSearch_OnServer()
    {
        using var db = new TestDb();
        await DbInitializer.SeedCatalogAsync(db.Context);
        var service = new ProductService(db.Context);

        Assert.Equal(3, (await service.GetAllAsync("Пчелен прашец", null, null, null)).Count);
        Assert.Equal(2, (await service.GetAllAsync(null, "Странджа", null, null)).Count);
        Assert.Single(await service.GetAllAsync(null, null, "manov", null));
        Assert.Single(await service.GetAllAsync(null, null, null, "Липов"));
        Assert.Single(await service.GetAllAsync("Мед", "Добруджа", null, null));
    }

    [Fact]
    public async Task Review_IsHiddenUntilApproved()
    {
        using var db = new TestDb();
        var service = new ReviewService(db.Context);

        var created = await service.CreateAsync(new CreateReviewDto
        {
            AuthorName = "Мария", City = "Бургас", Rating = 4, Comment = "Много хубав мед, препоръчвам!"
        });

        Assert.False(created.IsApproved);
        Assert.Empty(await service.GetApprovedAsync());

        await service.SetApprovedAsync(created.Id, true);

        var visible = Assert.Single(await service.GetApprovedAsync());
        Assert.Equal("Мария", visible.AuthorName);
    }

    [Fact]
    public async Task Review_ForUnknownProduct_IsRejected()
    {
        using var db = new TestDb();
        var service = new ReviewService(db.Context);

        await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(new CreateReviewDto
        {
            AuthorName = "Иван", Rating = 5, Comment = "Отзив за несъществуващ продукт", ProductId = 12345
        }));
        Assert.Empty(db.Context.Reviews);
    }

    [Fact]
    public async Task Promo_ExpiredOrInactiveCode_IsRejected()
    {
        using var db = new TestDb();
        db.Context.PromoCodes.AddRange(
            new PromoCode { Code = "OLD", DiscountPercent = 20, ValidUntil = DateTime.UtcNow.AddDays(-1) },
            new PromoCode { Code = "OFF", DiscountPercent = 20, IsActive = false });
        db.Context.SaveChanges();
        var service = new PromoService(db.Context);

        Assert.Equal(10, (await service.ValidateAsync(" bg10 ")).DiscountPercent);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => service.ValidateAsync("OLD"))).StatusCode);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => service.ValidateAsync("OFF"))).StatusCode);
    }
}
