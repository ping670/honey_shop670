using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pcheli_diplomenproekt.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string CustomerPhone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DeliveryCity { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Наложен платеж (при доставка)";

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Приета";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required]
    [MaxLength(150)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ApiaryName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Weight { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPrice { get; set; }
}
