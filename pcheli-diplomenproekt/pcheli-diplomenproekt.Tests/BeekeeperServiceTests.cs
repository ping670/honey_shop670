using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Tests;

public class BeekeeperServiceTests
{
    private static BeekeeperApplyDto NewApplication(string babh = "БАБХ-NEW-1") => new()
    {
        FullName = "Нов Пчелар", ApiaryName = "Нов Пчелин", RegistrationNumber = babh, Region = "Родопи",
        CityOrVillage = "с. Тест", Phone = "0899 123 456", OffersHoney = true, OffersPollen = true, HiveCount = 30
    };

    [Fact]
    public async Task Submit_DoesNotPublishBeekeeper_UntilApproved()
    {
        using var db = new TestDb();
        var service = new BeekeeperService(db.Context);

        await service.SubmitApplicationAsync(NewApplication());

        Assert.Equal(BeekeeperService.Pending, db.Context.BeekeeperApplications.Single().Status);
        Assert.Empty(db.Context.Beekeepers);
        Assert.Empty(db.Context.Products);
    }

    [Fact]
    public async Task Approve_CreatesBeekeeperAndProductsWithVariants()
    {
        using var db = new TestDb();
        var service = new BeekeeperService(db.Context);
        await service.SubmitApplicationAsync(NewApplication());
        var appId = db.Context.BeekeeperApplications.Single().Id;

        var dto = await service.ApproveAsync(appId);

        Assert.Equal("БАБХ-NEW-1", dto.RegistrationNumber);
        Assert.Equal(2, db.Context.Products.Count());               // мед + прашец
        Assert.All(db.Context.Products.Include(p => p.Variants), p => Assert.NotEmpty(p.Variants));
        Assert.Equal(BeekeeperService.Approved, db.Context.BeekeeperApplications.Single().Status);
    }

    [Fact]
    public async Task Approve_AlreadyProcessedApplication_IsRejected()
    {
        using var db = new TestDb();
        var service = new BeekeeperService(db.Context);
        await service.SubmitApplicationAsync(NewApplication());
        var appId = db.Context.BeekeeperApplications.Single().Id;
        await service.ApproveAsync(appId);

        await Assert.ThrowsAsync<BusinessException>(() => service.ApproveAsync(appId));
        Assert.Single(db.Context.Beekeepers);
    }

    [Fact]
    public async Task Approve_LinksApplicationToBeekeeper_AndKeepsRegion()
    {
        using var db = new TestDb();
        var service = new BeekeeperService(db.Context);
        await service.SubmitApplicationAsync(NewApplication());
        var appId = db.Context.BeekeeperApplications.Single().Id;

        var dto = await service.ApproveAsync(appId);

        Assert.Equal("Родопи", dto.Region);
        Assert.Equal(dto.Id, db.Context.BeekeeperApplications.Single().BeekeeperId);
        Assert.Equal(2, db.Context.Beekeepers.Single().RegionId);
    }

    [Fact]
    public async Task Reject_MarksApplication_AndPublishesNothing()
    {
        using var db = new TestDb();
        var service = new BeekeeperService(db.Context);
        await service.SubmitApplicationAsync(NewApplication());
        var appId = db.Context.BeekeeperApplications.Single().Id;

        await service.RejectAsync(appId);

        Assert.Equal(BeekeeperService.Rejected, db.Context.BeekeeperApplications.Single().Status);
        Assert.Empty(db.Context.Beekeepers);
        await Assert.ThrowsAsync<BusinessException>(() => service.ApproveAsync(appId));
    }

    [Fact]
    public async Task Submit_UnknownRegion_IsRejected()
    {
        using var db = new TestDb();
        var service = new BeekeeperService(db.Context);
        var dto = NewApplication();
        dto.Region = "Несъществуващ регион";

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.SubmitApplicationAsync(dto));

        Assert.Equal(400, ex.StatusCode);
        Assert.Empty(db.Context.BeekeeperApplications);
    }

    [Fact]
    public async Task GetAll_FiltersByRegionName()
    {
        using var db = new TestDb();
        await pcheli_diplomenproekt.Data.DbInitializer.SeedCatalogAsync(db.Context);
        var service = new BeekeeperService(db.Context);

        var all = await service.GetAllAsync(null);
        var rhodopes = await service.GetAllAsync("Родопи");

        Assert.Equal(6, all.Count);
        Assert.Single(rhodopes);
        Assert.Equal("Родопи", rhodopes[0].Region);
        Assert.Equal(2, rhodopes[0].ProductCount);
    }

    [Fact]
    public async Task Submit_DuplicateRegistrationNumber_IsRejected()
    {
        using var db = new TestDb();
        var service = new BeekeeperService(db.Context);
        await service.SubmitApplicationAsync(NewApplication("БАБХ-DUP"));

        await Assert.ThrowsAsync<BusinessException>(() => service.SubmitApplicationAsync(NewApplication("БАБХ-DUP")));
    }
}
