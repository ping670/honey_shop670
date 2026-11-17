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
}
