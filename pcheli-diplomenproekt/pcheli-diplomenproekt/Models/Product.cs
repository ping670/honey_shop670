using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pcheli_diplomenproekt.Models;

/// <summary>
/// Представлява предлаган пчелен продукт (Мед или Пчелен прашец). Цените и опаковките са във ProductVariant.
/// </summary>
public class Product
{
    public int Id { get; set; }

    public int BeekeeperId { get; set; }
    public Beekeeper? Beekeeper { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Type { get; set; } = "Мед"; // "Мед" или "Пчелен прашец"

    [Required, MaxLength(50)]
    public string Category { get; set; } = "herbal"; // manov, acacia, herbal, lavender, linden, pollen

    [Required, MaxLength(100)]
    public string Origin { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(60)]
    public string HarvestDate { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Purity { get; set; } = string.Empty;

    [MaxLength(200)]
    public string TastingNotes { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Color { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Aroma { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Crystallization { get; set; } = string.Empty;

    [MaxLength(250)]
    public string Image { get; set; } = "./images/acacia.jpg";

    public bool InStock { get; set; } = true;

    public double Rating { get; set; } = 5.0;

    [MaxLength(60)]
    public string Tag { get; set; } = "100% Натурален";

    /// <summary>Тагове за препоръките от въпросника, разделени със запетая (напр. "immunity,herbal").</summary>
    [MaxLength(200)]
    public string QuizTags { get; set; } = string.Empty;

    /// <summary>За подаръчни комплекти – какво съдържа комплектът (свободен текст).</summary>
    [MaxLength(500)]
    public string Contents { get; set; } = string.Empty;

    // ----- Произход и сертификати -----
    [MaxLength(60)]
    public string BatchNumber { get; set; } = string.Empty;

    public int? HarvestYear { get; set; }

    [MaxLength(250)]
    public string LabReportUrl { get; set; } = string.Empty;

    /// <summary>Кратко резюме на лабораторния анализ (напр. „Влага 17.2%; HMF 8 mg/kg; диастазно число 14“).</summary>
    [MaxLength(400)]
    public string LabSummary { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Certificates { get; set; } = string.Empty;

    // ----- Английски текстове (по желание; ако липсват, се показват българските) -----
    [MaxLength(150)]
    public string NameEn { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string DescriptionEn { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}

/// <summary>Опаковка на продукт (напр. „Буркан 0.9 кг“) със собствена цена.</summary>
public class ProductVariant
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, MaxLength(80)]
    public string Label { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Weight { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public bool InStock { get; set; } = true;

    /// <summary>Наличен брой. Намалява се при поръчка и се връща при отказана поръчка; при 0 опаковката става „изчерпана“.</summary>
    public int StockQuantity { get; set; } = 100;

    /// <summary>Тегло в кг, изчислено от полето Weight („0.9kg“, „250g“); използва се за цената на доставка.</summary>
    [NotMapped]
    public decimal WeightKg => ParseWeightKg(Weight);

    public static decimal ParseWeightKg(string? weight)
    {
        if (string.IsNullOrWhiteSpace(weight)) return 0.5m;
        var w = weight.Trim().ToLowerInvariant().Replace(',', '.').Replace(" ", "");
        var isGrams = w.EndsWith("g") && !w.EndsWith("kg");
        var digits = new string(w.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        if (!decimal.TryParse(digits, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var n) || n <= 0)
            return 0.5m;
        return isGrams ? n / 1000m : n;
    }
}
