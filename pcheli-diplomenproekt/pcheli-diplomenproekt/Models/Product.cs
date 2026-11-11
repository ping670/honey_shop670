using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pcheli_diplomenproekt.Models;

/// <summary>
/// Представлява предлаган пчелен продукт (Мед или Пчелен прашец)
/// </summary>
public class Product
{
    public int Id { get; set; }

    public int BeekeeperId { get; set; }
    public Beekeeper? Beekeeper { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Type { get; set; } = "Мед"; // "Мед" или "Пчелен прашец"

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "honey"; // "manov", "acacia", "herbal", "lavender", "linden", "pollen_fresh", "pollen_dried"

    [Required]
    [MaxLength(100)]
    public string Origin { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(60)]
    public string HarvestDate { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Purity { get; set; } = string.Empty;

    [MaxLength(200)]
    public string TastingNotes { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [MaxLength(50)]
    public string Weight { get; set; } = "0.9 кг";

    [MaxLength(250)]
    public string Image { get; set; } = "./images/manov.jpg";

    public bool InStock { get; set; } = true;

    public double Rating { get; set; } = 5.0;

    public string Tag { get; set; } = "100% Натурален";
}
