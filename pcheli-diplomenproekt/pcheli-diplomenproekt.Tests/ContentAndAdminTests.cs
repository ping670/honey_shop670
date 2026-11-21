using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Models;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Tests;

/// <summary>Блог, експорт на поръчки, табло и профил на пчелара.</summary>
public class ContentAndAdminTests
{
    // ---------- Блог ----------
    [Theory]
    [InlineData("Как да разпознаем истинския мед", "kak-da-razpoznaem-istinskiya-med")]
    [InlineData("Мед & Прашец: 100% натурално!", "med-prashets-100-naturalno")]
    [InlineData("Щастие", "shtastie")]
    [InlineData("!!!", "statia")]
    public void Slugify_TransliteratesBulgarian(string title, string expected) =>
        Assert.Equal(expected, ArticleService.Slugify(title));

    [Fact]
    public async Task Articles_GetUniqueSlugs_AndOnlyPublishedAreListed()
    {
        using var db = new TestDb();
        var svc = new ArticleService(db.Context);
        var dto = () => new ArticleUpsertDto { Title = "Мед", Summary = "s", Content = "Текст" };

        var a = await svc.CreateAsync(dto());
        var b = await svc.CreateAsync(dto());
        var draftDto = dto(); draftDto.IsPublished = false; draftDto.Title = "Чернова";
        var draft = await svc.CreateAsync(draftDto);

        Assert.Equal("med", a.Slug);
        Assert.Equal("med-2", b.Slug);
        var (items, total) = await svc.GetPublishedAsync(1, 10, null);
        Assert.Equal(2, total);
        Assert.DoesNotContain(items, i => i.Slug == draft.Slug);
        await Assert.ThrowsAsync<BusinessException>(() => svc.GetBySlugAsync(draft.Slug));          // публично – 404
        Assert.Equal("Чернова", (await svc.GetBySlugAsync(draft.Slug, includeUnpublished: true)).Title);
    }

    [Fact]
    public async Task Article_Update_KeepsSlugUnlessChanged_AndRejectsUnsafeCover()
    {
        using var db = new TestDb();
        var svc = new ArticleService(db.Context);
        var a = await svc.CreateAsync(new ArticleUpsertDto { Title = "Първа", Content = "x" });

        var updated = await svc.UpdateAsync(a.Id, new ArticleUpsertDto { Title = "Променена", Content = "y" });
        Assert.Equal(a.Slug, updated.Slug);
        Assert.Equal("Променена", updated.Title);

        await Assert.ThrowsAsync<BusinessException>(() =>
            svc.CreateAsync(new ArticleUpsertDto { Title = "Лоша", Content = "x", CoverImage = "javascript:alert(1)" }));
    }

    [Fact]
    public void ArticleRenderer_EncodesEverything_AndBuildsParagraphsHeadingsAndLists()
    {
        var html = ArticleRenderer.ToHtml("""
            Първи абзац <script>alert(1)</script>

            ## Подзаглавие & още
            - първо
            - <b>второ</b>

            Втори абзац
            със нов ред
            """);

        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<b>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("<h2>Подзаглавие &amp; още</h2>", html);
        Assert.Contains("<ul>", html);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(html, "<li>").Count);
        Assert.Contains("Втори абзац<br>със нов ред", html);
    }

    [Fact]
    public async Task SeededArticles_ArePublished_Once()
    {
        using var db = new TestDb();
        await SeedExtras.SeedArticlesAsync(db.Context);
        await SeedExtras.SeedArticlesAsync(db.Context);

        var (items, total) = await new ArticleService(db.Context).GetPublishedAsync(1, 10, null);
        Assert.Equal(3, total);
        Assert.All(items, i => Assert.False(string.IsNullOrWhiteSpace(i.Summary)));
        var recipes = await new ArticleService(db.Context).GetPublishedAsync(1, 10, "Рецепти");
        Assert.Single(recipes.Items);
    }

    // ---------- Експорт ----------
    [Fact]
    public void Csv_HasBom_EscapesSemicolonsAndQuotes_AndNeutralisesFormulas()
    {
        var order = new OrderDto(1, "BG-1", "=HYPERLINK(\"evil\")", "+359 888", "a@b.c", "София", "ул. \"Тест\"; 5", "бележка\nс нов ред", "Наложен", null,
            20m, 0m, 3m, 23m, "Приета", new DateTime(2026, 11, 18, 10, 30, 0, DateTimeKind.Utc),
            new List<OrderItemDto> { new(1, 1, "Мед", "Пчелин", "0.9kg", 2, 10m, 20m) }, "Наложен платеж", "Еконт", "Офис", true, "Поздрав; с точка и запетая");

        var bytes = OrderExport.ToCsv(new[] { order });
        var text = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());         // BOM за кирилица в Excel
        Assert.Contains("'=HYPERLINK", text);                                           // не се изпълнява като формула
        Assert.Contains("\"ул. \"\"Тест\"\"; 5\"", text);                              // кавички и ; са екранирани
        Assert.Contains("'+359 888", text);
        Assert.DoesNotContain("бележка\nс", text);                                       // няма нови редове в клетка
        Assert.Equal(2, text.TrimEnd().Split('\n').Length);                              // заглавен ред + един ред
        Assert.Contains("23,00", text);                                                  // български десетичен формат
    }

    [Fact]
    public void Labels_EncodeUserDataAndShowCashOnDeliveryOnlyWhenUnpaid()
    {
        OrderDto Mk(string payment, string status) => new(1, "BG-1", "<img src=x onerror=alert(1)>", "0888", "", "София", "ул. 1", null, payment, null,
            10m, 0m, 3m, 13m, "Приета", DateTime.UtcNow, new List<OrderItemDto> { new(1, 1, "Мед", "Пчелин", "1kg", 1, 10m, 10m) }, status, "Еконт", "Офис");

        var html = OrderExport.LabelsHtml(new[] { Mk("Наложен платеж", "Наложен платеж"), Mk("Банкова карта", "Платена") }, "Пчелари", "0888 123 456");

        Assert.DoesNotContain("<img src=x", html);
        Assert.Contains("&lt;img", html);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "НАЛОЖЕН ПЛАТЕЖ: "));
        Assert.Contains("ПЛАТЕНО", html);
    }

    // ---------- Табло ----------
    [Fact]
    public async Task Dashboard_ExcludesCancelledOrders_AndAggregatesTopProducts()
    {
        using var db = new TestDb();
        var (_, product) = db.SeedProduct();
        var svc = Make.Orders(db);
        var v = product.Variants.First(x => x.Price == 20.00m);
        await svc.CreateAsync(Make.Order(v.Id, 3));                       // 60 €, безплатна доставка
        await svc.CreateAsync(Make.Order(v.Id, 1));                       // 20 € + 3 € доставка
        await svc.CreateAsync(Make.Order(v.Id, 2));
        await svc.UpdateStatusAsync(db.Context.Orders.OrderBy(o => o.Id).Last().Id, OrderService.Cancelled);

        var d = await new StatsService(db.Context).GetDashboardAsync();

        Assert.Equal(83.00m, d.TotalRevenue);                              // 60 + 23, отказаната не се брои
        Assert.Equal(2, d.OrdersTotal);
        Assert.Equal(41.50m, d.AverageOrderValue);
        Assert.Equal(12, d.Monthly.Count);
        Assert.Equal(83.00m, d.Monthly.Last().Revenue);
        Assert.Equal(4, d.TopProducts.Single().Quantity);
        Assert.Equal("Тест Пчелин", d.TopBeekeepers.Single().ApiaryName);
        Assert.Equal(OrderService.Statuses.Length, d.ByStatus.Count);
        Assert.Equal(1, d.ByStatus.Single(s => s.Status == OrderService.Cancelled).Count);
        Assert.Equal(2, d.PendingOrders);
    }

    [Fact]
    public async Task Dashboard_OnEmptyDatabase_ReturnsZeros()
    {
        using var db = new TestDb();

        var d = await new StatsService(db.Context).GetDashboardAsync();

        Assert.Equal(0m, d.TotalRevenue);
        Assert.Equal(0m, d.AverageOrderValue);
        Assert.Equal(12, d.Monthly.Count);
        Assert.Empty(d.TopProducts);
    }

    // ---------- Пчелари ----------
    [Fact]
    public async Task Approve_LinksApplicantAccount_AndSendsNotifications()
    {
        using var db = new TestDb();
        var user = Make.User(db, "pchelar@example.com");
        var notifier = new FakeNotifier();
        var svc = new BeekeeperService(db.Context, notifier);
        await svc.SubmitApplicationAsync(new BeekeeperApplyDto
        {
            FullName = "Нов", ApiaryName = "Нов Пчелин", RegistrationNumber = "БАБХ-9", Region = "Родопи", CityOrVillage = "с. Т",
            Phone = "0888 1", Email = "pchelar@example.com", OffersHoney = true
        }, user.Id);

        var dto = await svc.ApproveAsync(db.Context.BeekeeperApplications.Single().Id);

        Assert.Equal(user.Id, db.Context.Beekeepers.Single().UserId);
        Assert.Equal(dto.Id, await svc.GetIdByUserAsync(user.Id));
        Assert.Equal(("Нов Пчелин", true), Assert.Single(notifier.Decisions));
    }

    [Fact]
    public async Task Reject_NotifiesApplicant()
    {
        using var db = new TestDb();
        var notifier = new FakeNotifier();
        var svc = new BeekeeperService(db.Context, notifier);
        await svc.SubmitApplicationAsync(new BeekeeperApplyDto
        { FullName = "Н", ApiaryName = "П", RegistrationNumber = "R-1", Region = "Родопи", CityOrVillage = "с. Т", Phone = "1" });

        await svc.RejectAsync(db.Context.BeekeeperApplications.Single().Id);

        Assert.False(Assert.Single(notifier.Decisions).Approved);
    }

    [Fact]
    public async Task UpdateProfile_SavesStoryAndPhotos_AndRejectsUnsafeUrls()
    {
        using var db = new TestDb();
        var (bk, _) = db.SeedProduct();
        var svc = new BeekeeperService(db.Context);

        var dto = await svc.UpdateProfileAsync(bk.Id, new BeekeeperProfileUpdateDto
        {
            Bio = "Кратко", Story = "Дълга история", PhotoUrl = "/uploads/me.jpg",
            ApiaryPhotos = { "/uploads/a.jpg", "https://example.com/b.jpg" }
        });
        Assert.Equal("/uploads/me.jpg", dto.PhotoUrl);

        var detail = await svc.GetDetailAsync(bk.Id);
        Assert.Equal("Дълга история", detail.Story);
        Assert.Equal(2, detail.ApiaryPhotos.Length);
        Assert.Single(detail.Products);

        await Assert.ThrowsAsync<BusinessException>(() => svc.UpdateProfileAsync(bk.Id, new BeekeeperProfileUpdateDto { PhotoUrl = "javascript:alert(1)" }));
        await Assert.ThrowsAsync<BusinessException>(() => svc.UpdateProfileAsync(bk.Id, new BeekeeperProfileUpdateDto { ApiaryPhotos = { "/a.jpg;/b.jpg" } }));
        await Assert.ThrowsAsync<BusinessException>(() => svc.UpdateProfileAsync(9999, new BeekeeperProfileUpdateDto()));
    }

    [Fact]
    public async Task Detail_AveragesApprovedReviewsOfOwnProductsOnly()
    {
        using var db = new TestDb();
        var (bk, product) = db.SeedProduct();
        db.Context.Reviews.AddRange(
            new Review { AuthorName = "А", Rating = 5, Comment = "Много хубав мед!!", IsApproved = true, ProductId = product.Id },
            new Review { AuthorName = "Б", Rating = 3, Comment = "Горе-долу става", IsApproved = true, ProductId = product.Id },
            new Review { AuthorName = "В", Rating = 1, Comment = "Неодобрен отзив", IsApproved = false, ProductId = product.Id },
            new Review { AuthorName = "Г", Rating = 1, Comment = "Отзив за нищо конкретно", IsApproved = true });
        db.Context.SaveChanges();

        var detail = await new BeekeeperService(db.Context).GetDetailAsync(bk.Id);

        Assert.Equal(2, detail.ReviewCount);
        Assert.Equal(4.0, detail.AverageRating);
    }

    [Fact]
    public async Task Panel_Orders_And_Account_Linking_RequireIsolation()
    {
        using var db = new TestDb();
        var (bk, _) = db.SeedProduct();
        var user = Make.User(db, "x@example.com");
        db.Context.Beekeepers.Add(new Beekeeper { FullName = "Друг", ApiaryName = "Друг", RegistrationNumber = "R-2", RegionId = 1, CityOrVillage = "с.", Phone = "1", UserId = user.Id });
        db.Context.SaveChanges();
        var svc = new BeekeeperService(db.Context);

        Assert.Null(await svc.GetIdByUserAsync("няма-такъв"));
        Assert.NotEqual(bk.Id, await svc.GetIdByUserAsync(user.Id));
        // без UserManager свързването на акаунт не е налично
        await Assert.ThrowsAsync<BusinessException>(() => svc.LinkAccountAsync(bk.Id, "x@example.com"));
    }
}
