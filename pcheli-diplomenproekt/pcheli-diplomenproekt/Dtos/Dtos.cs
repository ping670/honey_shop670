using System.ComponentModel.DataAnnotations;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Dtos;

// ---------- Изходни DTO ----------
public record VariantDto(int Id, string Label, string Weight, decimal Price, bool InStock, int Stock = 0);

public record BeekeeperSummaryDto(int Id, string FullName, string ApiaryName, string RegistrationNumber,
    string Region, string CityOrVillage, string Phone, double Rating);

public record ProductDto(int Id, string Name, string Type, string Category, string Origin, string Tag, string Image,
    string Description, string Color, string Aroma, string Crystallization, string HarvestDate, string Purity,
    string TastingNotes, double Rating, bool InStock, string[] QuizTags,
    List<VariantDto> Variants, BeekeeperSummaryDto? Beekeeper,
    string Contents = "", string BatchNumber = "", int? HarvestYear = null, string LabReportUrl = "",
    string LabSummary = "", string Certificates = "", string NameEn = "", string DescriptionEn = "",
    DateTime? CreatedAt = null);

public record BeekeeperDto(int Id, string FullName, string ApiaryName, string RegistrationNumber, string Region,
    string CityOrVillage, string Phone, string Email, string Bio, int ExperienceYears, int HiveCount, double Rating,
    bool IsVerified, bool HasHoney, bool HasPollen, int ProductCount, string PhotoUrl = "");

public record OrderResultDto(string OrderNumber, decimal Subtotal, decimal Discount, decimal Shipping, decimal Total, string Message,
    bool RequiresPayment = false, string PaymentStatus = "Наложен платеж");

public record OrderItemDto(int ProductId, int ProductVariantId, string ProductName, string ApiaryName, string Weight,
    int Quantity, decimal UnitPrice, decimal TotalPrice);

public record OrderDto(int Id, string OrderNumber, string CustomerName, string CustomerPhone, string CustomerEmail,
    string DeliveryCity, string DeliveryAddress, string? Notes, string PaymentMethod, string? PromoCode,
    decimal Subtotal, decimal DiscountAmount, decimal ShippingAmount, decimal TotalAmount, string Status,
    DateTime CreatedAt, List<OrderItemDto> Items,
    string PaymentStatus = "", string Courier = "", string DeliveryType = "", bool IsGift = false, string? GiftMessage = null,
    List<OrderEventDto>? Events = null, bool CanUpdateStatus = false);

public record ReviewDto(int Id, string AuthorName, string City, int Rating, string Comment, bool IsApproved,
    DateTime CreatedAt, int? ProductId, string? ProductName);

public record PromoValidationDto(string Code, int DiscountPercent);

public record PromoCodeDto(int Id, string Code, int DiscountPercent, bool IsActive, DateTime? ValidUntil);

public record RegionDto(int Id, string Name, string Description, int BeekeepersCount);

public record BeekeeperApplicationDto(int Id, string FullName, string ApiaryName, string RegistrationNumber,
    int RegionId, string Region, string CityOrVillage, string Phone, string? Email, bool OffersHoney, bool OffersPollen,
    string ProductDetails, int HiveCount, string Status, DateTime SubmittedAt, DateTime? ReviewedAt, int? BeekeeperId);

public record StatsDto(int BeekeepersCount, int ProductsCount, int HoneyCount, int PollenCount, int OrdersCount,
    int RegionsCount, int TotalHives);

// ---------- Входни DTO ----------
public class CreateOrderDto
{
    [Required(ErrorMessage = "Името е задължително"), MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Телефонът е задължителен"), MaxLength(30)]
    public string CustomerPhone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Невалиден имейл"), MaxLength(100)]
    public string? CustomerEmail { get; set; }

    [Required(ErrorMessage = "Градът е задължителен"), MaxLength(100)]
    public string DeliveryCity { get; set; } = string.Empty;

    [Required(ErrorMessage = "Адресът е задължителен"), MaxLength(250)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [MaxLength(40)]
    public string? PromoCode { get; set; }

    /// <summary>„cod“ (наложен платеж, по подразбиране) или „card“ (онлайн с карта през Stripe).</summary>
    [MaxLength(10)]
    public string? PaymentType { get; set; }

    /// <summary>„Еконт“ или „Спиди“.</summary>
    [MaxLength(30)]
    public string? Courier { get; set; }

    /// <summary>„Офис“ или „Адрес“.</summary>
    [MaxLength(20)]
    public string? DeliveryType { get; set; }

    public bool IsGift { get; set; }

    [MaxLength(300)]
    public string? GiftMessage { get; set; }

    [Required, MinLength(1, ErrorMessage = "Количката е празна")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

/// <summary>Заявка за изчисляване на цената (подбор на куриер, отстъпка, доставка) без създаване на поръчка.</summary>
public class QuoteRequestDto
{
    [MaxLength(40)] public string? PromoCode { get; set; }
    [MaxLength(30)] public string? Courier { get; set; }
    [MaxLength(20)] public string? DeliveryType { get; set; }

    [Required, MinLength(1, ErrorMessage = "Количката е празна")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public record QuoteDto(decimal Subtotal, decimal Discount, decimal Shipping, decimal Total, decimal FreeShippingFrom,
    decimal WeightKg, string Courier, string DeliveryType, string? PromoCode, int DiscountPercent);

public class CreateOrderItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Невалидна опаковка")]
    public int ProductVariantId { get; set; }

    [Range(1, 50, ErrorMessage = "Количеството трябва да е между 1 и 50")]
    public int Quantity { get; set; }
}

public class CreateReviewDto
{
    [Required(ErrorMessage = "Името е задължително"), MaxLength(100)]
    public string AuthorName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? City { get; set; }

    [Range(1, 5, ErrorMessage = "Оценката трябва да е между 1 и 5")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "Коментарът е задължителен"), MinLength(10, ErrorMessage = "Отзивът трябва да е поне 10 символа"),
     MaxLength(1500, ErrorMessage = "Отзивът може да е най-много 1500 символа")]
    public string Comment { get; set; } = string.Empty;

    /// <summary>По желание – продуктът, за който е отзивът.</summary>
    public int? ProductId { get; set; }
}

public class BeekeeperApplyDto
{
    [Required(ErrorMessage = "Името е задължително"), MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Името на пчелина е задължително"), MaxLength(120)]
    public string ApiaryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Регистрационният номер по БАБХ е задължителен"), MaxLength(50)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Изберете регион"), MaxLength(80)]
    public string Region { get; set; } = string.Empty;

    [Required(ErrorMessage = "Въведете населено място"), MaxLength(100)]
    public string CityOrVillage { get; set; } = string.Empty;

    [Required(ErrorMessage = "Телефонът за контакт е задължителен"), MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Невалиден имейл"), MaxLength(100)]
    public string? Email { get; set; }

    public bool OffersHoney { get; set; } = true;
    public bool OffersPollen { get; set; }

    [MaxLength(500)]
    public string? ProductDetails { get; set; }

    [Range(1, 100000, ErrorMessage = "Невалиден брой кошери")]
    public int HiveCount { get; set; } = 20;
}

public class ProductUpsertDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Изберете пчелар")]
    public int BeekeeperId { get; set; }

    [Required(ErrorMessage = "Името е задължително"), MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Type { get; set; } = "Мед";

    [Required, MaxLength(50)]
    public string Category { get; set; } = "herbal";

    [Required(ErrorMessage = "Произходът е задължителен"), MaxLength(100)]
    public string Origin { get; set; } = string.Empty;

    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    [MaxLength(60)] public string HarvestDate { get; set; } = string.Empty;
    [MaxLength(120)] public string Purity { get; set; } = string.Empty;
    [MaxLength(200)] public string TastingNotes { get; set; } = string.Empty;
    [MaxLength(80)] public string Color { get; set; } = string.Empty;
    [MaxLength(80)] public string Aroma { get; set; } = string.Empty;
    [MaxLength(80)] public string Crystallization { get; set; } = string.Empty;
    [MaxLength(250)] public string Image { get; set; } = "./images/acacia.jpg";
    [MaxLength(60)] public string Tag { get; set; } = "100% Натурален";
    [MaxLength(200)] public string QuizTags { get; set; } = string.Empty;
    public bool InStock { get; set; } = true;

    // Подаръчни комплекти, произход и сертификати, английски текстове
    [MaxLength(500)] public string Contents { get; set; } = string.Empty;
    [MaxLength(60)] public string BatchNumber { get; set; } = string.Empty;
    [Range(1990, 2100, ErrorMessage = "Невалидна година на реколтата")] public int? HarvestYear { get; set; }
    [MaxLength(250)] public string LabReportUrl { get; set; } = string.Empty;
    [MaxLength(400)] public string LabSummary { get; set; } = string.Empty;
    [MaxLength(300)] public string Certificates { get; set; } = string.Empty;
    [MaxLength(150)] public string NameEn { get; set; } = string.Empty;
    [MaxLength(2000)] public string DescriptionEn { get; set; } = string.Empty;

    [Required, MinLength(1, ErrorMessage = "Добавете поне една опаковка")]
    public List<VariantUpsertDto> Variants { get; set; } = new();
}

public class VariantUpsertDto
{
    [Required, MaxLength(80)] public string Label { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Weight { get; set; } = string.Empty;
    [Range(0.01, 100000, ErrorMessage = "Невалидна цена")] public decimal Price { get; set; }
    public bool InStock { get; set; } = true;

    /// <summary>Наличен брой; ако не е подаден, при ново създадена опаковка е 100, а при съществуваща не се променя.</summary>
    [Range(0, 1000000, ErrorMessage = "Невалидна наличност")] public int? StockQuantity { get; set; }
}

public class PromoCodeUpsertDto
{
    [Required(ErrorMessage = "Кодът е задължителен"), MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Отстъпката трябва да е между 1 и 100%")]
    public int DiscountPercent { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? ValidUntil { get; set; }
}

public class OrderStatusDto
{
    [Required, MaxLength(30)]
    public string Status { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required(ErrorMessage = "Въведете потребителско име")] public string Username { get; set; } = string.Empty;
    [Required(ErrorMessage = "Въведете парола")] public string Password { get; set; } = string.Empty;
}

// ---------- Мапиране ----------
public static class Mapping
{
    public static ProductDto ToDto(this Product p) => new(
        p.Id, p.Name, p.Type, p.Category, p.Origin, p.Tag, p.Image, p.Description, p.Color, p.Aroma,
        p.Crystallization, p.HarvestDate, p.Purity, p.TastingNotes, p.Rating, p.InStock,
        p.QuizTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        p.Variants.OrderBy(v => v.Id).Select(v => new VariantDto(v.Id, v.Label, v.Weight, v.Price, v.InStock && v.StockQuantity > 0, v.StockQuantity)).ToList(),
        p.Beekeeper == null ? null : new BeekeeperSummaryDto(p.Beekeeper.Id, p.Beekeeper.FullName, p.Beekeeper.ApiaryName,
            p.Beekeeper.RegistrationNumber, p.Beekeeper.Region?.Name ?? string.Empty, p.Beekeeper.CityOrVillage,
            p.Beekeeper.Phone, p.Beekeeper.Rating),
        p.Contents, p.BatchNumber, p.HarvestYear, p.LabReportUrl, p.LabSummary, p.Certificates, p.NameEn,
        p.DescriptionEn, p.CreatedAt);

    public static ReviewDto ToDto(this Review r) =>
        new(r.Id, r.AuthorName, r.City, r.Rating, r.Comment, r.IsApproved, r.CreatedAt, r.ProductId, r.Product?.Name);

    public static PromoCodeDto ToDto(this PromoCode p) => new(p.Id, p.Code, p.DiscountPercent, p.IsActive, p.ValidUntil);

    public static BeekeeperApplicationDto ToDto(this BeekeeperApplication a) => new(a.Id, a.FullName, a.ApiaryName,
        a.RegistrationNumber, a.RegionId, a.Region?.Name ?? string.Empty, a.CityOrVillage, a.Phone, a.Email,
        a.OffersHoney, a.OffersPollen, a.ProductDetails, a.HiveCount, a.Status, a.SubmittedAt, a.ReviewedAt, a.BeekeeperId);

    public static OrderDto ToDto(this Order o) => new(
        o.Id, o.OrderNumber, o.CustomerName, o.CustomerPhone, o.CustomerEmail, o.DeliveryCity, o.DeliveryAddress,
        o.Notes, o.PaymentMethod, o.PromoCode, o.Subtotal, o.DiscountAmount, o.ShippingAmount, o.TotalAmount,
        o.Status, o.CreatedAt,
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductVariantId, i.ProductName, i.ApiaryName, i.Weight,
            i.Quantity, i.UnitPrice, i.TotalPrice)).ToList(),
        o.PaymentStatus, o.Courier, o.DeliveryType, o.IsGift, o.GiftMessage,
        o.Events.OrderBy(e => e.At).ThenBy(e => e.Id).Select(e => new OrderEventDto(e.Status, e.Note, e.At)).ToList());
}
