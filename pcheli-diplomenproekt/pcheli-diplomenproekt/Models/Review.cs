using System.ComponentModel.DataAnnotations;

namespace pcheli_diplomenproekt.Models;

/// <summary>Клиентски отзив. Публикува се на сайта едва след одобрение от администратор.</summary>
public class Review
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string AuthorName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [Required, MaxLength(1500)]
    public string Comment { get; set; } = string.Empty;

    public bool IsApproved { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>По желание – продуктът, за който е отзивът.</summary>
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
}
