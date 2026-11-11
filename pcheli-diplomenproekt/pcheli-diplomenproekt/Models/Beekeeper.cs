using System.ComponentModel.DataAnnotations;

namespace pcheli_diplomenproekt.Models;

/// <summary>
/// Представлява регистриран пчелар / пчелин в националната платформа „Пчелари България“
/// </summary>
public class Beekeeper
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string ApiaryName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string RegistrationNumber { get; set; } = string.Empty; // БАБХ номер

    [Required]
    [MaxLength(80)]
    public string Region { get; set; } = string.Empty; // Странджа, Родопи, Тракия, Добруджа, Дунавска равнина, Стара планина

    [Required]
    [MaxLength(100)]
    public string CityOrVillage { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Bio { get; set; } = string.Empty;

    public int ExperienceYears { get; set; } = 5;

    public int HiveCount { get; set; } = 50;

    public double Rating { get; set; } = 5.0;

    public bool IsVerified { get; set; } = true;

    public bool HasHoney { get; set; } = true;

    public bool HasPollen { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
