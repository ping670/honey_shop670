using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Data;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Tests;

/// <summary>SQLite in-memory база за тестове – една връзка за целия живот на теста.</summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public ApplicationDbContext Context { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    /// <summary>Пчелар с един продукт и две опаковки: 10.00 € и 20.00 €.</summary>
    public (Beekeeper Beekeeper, Product Product) SeedProduct(bool inStock = true)
    {
        var bk = new Beekeeper
        {
            FullName = "Тест Пчелар", ApiaryName = "Тест Пчелин", RegistrationNumber = "БАБХ-TEST-1",
            RegionId = 1 /* Странджа – сийднат с HasData */, CityOrVillage = "гр. Тест", Phone = "0888 000 000"
        };
        Context.Beekeepers.Add(bk);
        Context.SaveChanges();

        var product = new Product
        {
            BeekeeperId = bk.Id, Name = "Тестов мед", Type = "Мед", Category = "herbal", Origin = "Странджа", InStock = inStock,
            Variants =
            {
                new ProductVariant { Label = "Буркан 0.5 кг", Weight = "0.5kg", Price = 10.00m },
                new ProductVariant { Label = "Буркан 0.9 кг", Weight = "0.9kg", Price = 20.00m }
            }
        };
        Context.Products.Add(product);
        Context.SaveChanges();
        return (bk, product);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
