using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pcheli_diplomenproekt.Models;

public class Order
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string CustomerPhone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string DeliveryCity { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Наложен платеж (при доставка)";

    [MaxLength(40)]
    public string? PromoCode { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ShippingAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Приета";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Клиентът, ако е бил влязъл в акаунта си при поръчката.</summary>
    [MaxLength(450)]
    public string? UserId { get; set; }

    /// <summary>„Наложен платеж“, „Чака плащане“, „Платена“ или „Неуспешно плащане“.</summary>
    [MaxLength(30)]
    public string PaymentStatus { get; set; } = "Наложен платеж";

    [MaxLength(120)]
    public string? StripeSessionId { get; set; }

    [MaxLength(30)]
    public string Courier { get; set; } = "Еконт";

    /// <summary>„Офис“ или „Адрес“.</summary>
    [MaxLength(20)]
    public string DeliveryType { get; set; } = "Офис";

    public bool IsGift { get; set; }

    [MaxLength(300)]
    public string? GiftMessage { get; set; }

    /// <summary>Ако поръчката е създадена автоматично от абонамент.</summary>
    public int? SubscriptionId { get; set; }

    /// <summary>Наличностите вече са върнати (при отказ) – пази от двойно връщане.</summary>
    public bool StockRestored { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderEvent> Events { get; set; } = new List<OrderEvent>();
}

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    [Required, MaxLength(150)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ApiaryName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Weight { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPrice { get; set; }
}
