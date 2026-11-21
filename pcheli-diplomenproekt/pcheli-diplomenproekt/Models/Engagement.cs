using System.ComponentModel.DataAnnotations;

namespace pcheli_diplomenproekt.Models;

/// <summary>Съобщение от формата „Контакти“ (към магазина или към конкретен пчелар).</summary>
public class ContactMessage
{
    public int Id { get; set; }

    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Email { get; set; } = string.Empty;
    [MaxLength(30)] public string? Phone { get; set; }
    [Required, MaxLength(150)] public string Subject { get; set; } = string.Empty;
    [Required, MaxLength(2000)] public string Message { get; set; } = string.Empty;

    /// <summary>Ако е зададен, съобщението е към този пчелар (препраща се и на имейла му).</summary>
    public int? BeekeeperId { get; set; }
    public Beekeeper? Beekeeper { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsHandled { get; set; }
}

/// <summary>Абонат за бюлетина. Токенът служи за отписване с един клик от писмото.</summary>
public class NewsletterSubscriber
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Заявка „Уведоми ме, когато се върне в наличност“.</summary>
public class StockAlert
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [Required, MaxLength(100)] public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
