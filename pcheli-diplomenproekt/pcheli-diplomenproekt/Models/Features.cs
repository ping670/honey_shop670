using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace pcheli_diplomenproekt.Models;

/// <summary>Потребител на платформата (клиент, пчелар или администратор).</summary>
public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Запис в историята на поръчката (за страницата „Проследяване“).</summary>
public class OrderEvent
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Note { get; set; }

    public DateTime At { get; set; } = DateTime.UtcNow;
}

/// <summary>Запазен адрес за доставка на клиент.</summary>
public class SavedAddress
{
    public int Id { get; set; }

    [Required, MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string Label { get; set; } = "Домашен";

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string Address { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}

/// <summary>Любим продукт на клиент.</summary>
public class WishlistItem
{
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Абонамент за регулярна доставка на мед (наложен платеж).</summary>
public class Subscription
{
    public const string Active = "Активен";
    public const string Paused = "На пауза";
    public const string Cancelled = "Прекратен";

    public int Id { get; set; }

    [Required, MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public int Quantity { get; set; } = 1;

    /// <summary>На колко месеца се доставя (1, 2 или 3).</summary>
    public int IntervalMonths { get; set; } = 1;

    public DateTime NextDeliveryDate { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = Active;

    [Required, MaxLength(100)] public string CustomerName { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string CustomerPhone { get; set; } = string.Empty;
    [MaxLength(100)] public string CustomerEmail { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string DeliveryCity { get; set; } = string.Empty;
    [Required, MaxLength(250)] public string DeliveryAddress { get; set; } = string.Empty;
    [MaxLength(30)] public string Courier { get; set; } = "Еконт";
    [MaxLength(20)] public string DeliveryType { get; set; } = "Офис";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastOrderAt { get; set; }
    public int OrdersCreated { get; set; }
}

/// <summary>Статия в блога.</summary>
public class Article
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(400)]
    public string Summary { get; set; } = string.Empty;

    /// <summary>Текст на статията. Абзаците са разделени с празен ред; „## “ в началото на ред е подзаглавие.</summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(250)]
    public string CoverImage { get; set; } = "./images/hero.png";

    [MaxLength(100)]
    public string Author { get; set; } = "Екипът на Пчелари България";

    [MaxLength(60)]
    public string Category { get; set; } = "Здраве";

    public bool IsPublished { get; set; } = true;

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Журнал на промените, направени от администратори и пчелари.</summary>
public class AuditLog
{
    public long Id { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Method { get; set; } = string.Empty;

    /// <summary>Описание на действието, напр. „Admin/UpdateProduct“.</summary>
    [MaxLength(120)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Path { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? EntityId { get; set; }

    public int StatusCode { get; set; }

    [MaxLength(45)]
    public string? Ip { get; set; }

    [MaxLength(500)]
    public string? Details { get; set; }
}
