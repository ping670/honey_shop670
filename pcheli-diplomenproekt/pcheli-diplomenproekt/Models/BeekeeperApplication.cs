using System.ComponentModel.DataAnnotations;

namespace pcheli_diplomenproekt.Models;

/// <summary>
/// Заявка за регистрация на нов пчелар и предлагане на пчелни продукти (мед и прашец)
/// </summary>
public class BeekeeperApplication
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Името е задължително")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Името на пчелина е задължително")]
    [MaxLength(120)]
    public string ApiaryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Регистрационният номер по БАБХ е задължителен")]
    [MaxLength(50)]
    public string RegistrationNumber { get; set; } = string.Empty;

    // Ако заявката е одобрена – създаденият пчелар
    public int? BeekeeperId { get; set; }
    public Beekeeper? Beekeeper { get; set; }

    public int RegionId { get; set; }
    public Region? Region { get; set; }

    [Required(ErrorMessage = "Въведете населено място")]
    [MaxLength(100)]
    public string CityOrVillage { get; set; } = string.Empty;

    [Required(ErrorMessage = "Телефонът за контакт е задължителен")]
    [MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    public bool OffersHoney { get; set; } = true;

    public bool OffersPollen { get; set; } = false;

    [MaxLength(500)]
    public string ProductDetails { get; set; } = string.Empty;

    public int HiveCount { get; set; } = 20;

    [MaxLength(30)]
    public string Status { get; set; } = "В обработка";

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Ако кандидатът е влязъл в акаунта си, при одобрение той получава роля Beekeeper и достъп до панела.</summary>
    [MaxLength(450)]
    public string? UserId { get; set; }
}
