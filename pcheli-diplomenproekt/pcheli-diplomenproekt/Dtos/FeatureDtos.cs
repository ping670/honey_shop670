using System.ComponentModel.DataAnnotations;

namespace pcheli_diplomenproekt.Dtos;

// ===================== Акаунти =====================
public class RegisterDto
{
    [Required(ErrorMessage = "Името е задължително"), MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Имейлът е задължителен"), EmailAddress(ErrorMessage = "Невалиден имейл"), MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Паролата е задължителна"), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }
}

public class AccountLoginDto
{
    [Required(ErrorMessage = "Въведете имейл")] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Въведете парола")] public string Password { get; set; } = string.Empty;
}

public class ProfileUpdateDto
{
    [Required(ErrorMessage = "Името е задължително"), MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }
}

public class ChangePasswordDto
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string NewPassword { get; set; } = string.Empty;
}

public record AccountDto(string Id, string Email, string FullName, string? Phone, string[] Roles, int? BeekeeperId, bool EmailConfirmed = true);

public class CancelReasonDto
{
    [MaxLength(180)] public string? Reason { get; set; }
}

public class AddressUpsertDto
{
    [Required(ErrorMessage = "Въведете име на адреса"), MaxLength(60)] public string Label { get; set; } = "Домашен";
    [Required(ErrorMessage = "Името е задължително"), MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Телефонът е задължителен"), MaxLength(30)] public string Phone { get; set; } = string.Empty;
    [Required(ErrorMessage = "Градът е задължителен"), MaxLength(100)] public string City { get; set; } = string.Empty;
    [Required(ErrorMessage = "Адресът е задължителен"), MaxLength(250)] public string Address { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public record AddressDto(int Id, string Label, string FullName, string Phone, string City, string Address, bool IsDefault);

public class WishlistMergeDto
{
    [MaxLength(200)] public List<int> ProductIds { get; set; } = new();
}

// ===================== Абонаменти =====================
public class SubscriptionCreateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Изберете опаковка")] public int ProductVariantId { get; set; }
    [Range(1, 20, ErrorMessage = "Количеството трябва да е между 1 и 20")] public int Quantity { get; set; } = 1;
    [Range(1, 3, ErrorMessage = "Доставка на всеки 1, 2 или 3 месеца")] public int IntervalMonths { get; set; } = 1;

    [Required(ErrorMessage = "Името е задължително"), MaxLength(100)] public string CustomerName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Телефонът е задължителен"), MaxLength(30)] public string CustomerPhone { get; set; } = string.Empty;
    [EmailAddress(ErrorMessage = "Невалиден имейл"), MaxLength(100)] public string? CustomerEmail { get; set; }
    [Required(ErrorMessage = "Градът е задължителен"), MaxLength(100)] public string DeliveryCity { get; set; } = string.Empty;
    [Required(ErrorMessage = "Адресът е задължителен"), MaxLength(250)] public string DeliveryAddress { get; set; } = string.Empty;
    [MaxLength(30)] public string? Courier { get; set; }
    [MaxLength(20)] public string? DeliveryType { get; set; }

    /// <summary>Дата на първата доставка (по подразбиране след 7 дни).</summary>
    public DateTime? FirstDeliveryDate { get; set; }
}

public class SubscriptionStatusDto
{
    /// <summary>„pause“, „resume“ или „cancel“.</summary>
    [Required, MaxLength(10)] public string Action { get; set; } = string.Empty;
}

public record SubscriptionDto(int Id, int ProductVariantId, string ProductName, string VariantLabel, decimal UnitPrice,
    int Quantity, int IntervalMonths, DateTime NextDeliveryDate, string Status, string DeliveryCity, string DeliveryAddress,
    string Courier, string DeliveryType, int OrdersCreated, DateTime? LastOrderAt, int DiscountPercent);

// ===================== Проследяване =====================
public record OrderEventDto(string Status, string? Note, DateTime At);

public record TrackingDto(string OrderNumber, string Status, string PaymentStatus, string PaymentMethod, DateTime CreatedAt,
    string Courier, string DeliveryType, string DeliveryCity, decimal Total, bool IsGift,
    List<OrderItemDto> Items, List<OrderEventDto> Events);

// ===================== Доставка и плащане =====================
public record CourierOptionDto(string Name, decimal OfficePrice, decimal AddressPrice);

public record PublicConfigDto(bool CardPaymentsEnabled, bool CaptchaEnabled, decimal FreeShippingFrom,
    int SubscriptionDiscountPercent, List<CourierOptionDto> Couriers, ShopInfoDto? Shop = null);

public record PaymentSessionDto(string Url);

// ===================== Блог =====================
public record ArticleListDto(int Id, string Slug, string Title, string Summary, string CoverImage, string Author,
    string Category, DateTime PublishedAt);

public record ArticleDto(int Id, string Slug, string Title, string Summary, string Content, string CoverImage,
    string Author, string Category, bool IsPublished, DateTime PublishedAt);

public class ArticleUpsertDto
{
    [Required(ErrorMessage = "Заглавието е задължително"), MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(120)] public string? Slug { get; set; }
    [MaxLength(400)] public string Summary { get; set; } = string.Empty;
    [Required(ErrorMessage = "Текстът е задължителен"), MaxLength(40000)] public string Content { get; set; } = string.Empty;
    [MaxLength(250)] public string CoverImage { get; set; } = "./images/hero.png";
    [MaxLength(100)] public string Author { get; set; } = "Екипът на Пчелари България";
    [MaxLength(60)] public string Category { get; set; } = "Здраве";
    public bool IsPublished { get; set; } = true;
}

// ===================== Пчелари: страница и панел =====================
public record BeekeeperReviewDto(int Id, string AuthorName, string City, int Rating, string Comment, DateTime CreatedAt, string? ProductName);

public record BeekeeperDetailDto(BeekeeperDto Beekeeper, string Story, string[] ApiaryPhotos, List<ProductDto> Products,
    List<BeekeeperReviewDto> Reviews, double AverageRating, int ReviewCount);

public class BeekeeperProfileUpdateDto
{
    [MaxLength(30)] public string? Phone { get; set; }
    [EmailAddress(ErrorMessage = "Невалиден имейл"), MaxLength(100)] public string? Email { get; set; }
    [MaxLength(1000)] public string Bio { get; set; } = string.Empty;
    [MaxLength(3000)] public string Story { get; set; } = string.Empty;
    [MaxLength(250)] public string PhotoUrl { get; set; } = string.Empty;
    [MaxLength(10)] public List<string> ApiaryPhotos { get; set; } = new();
}

public class LinkBeekeeperAccountDto
{
    [Required, EmailAddress, MaxLength(100)] public string Email { get; set; } = string.Empty;
}

// ===================== Наличности =====================
public record InventoryItemDto(int VariantId, int ProductId, string ProductName, string Label, string ApiaryName,
    int StockQuantity, bool InStock, bool IsLow);

public class StockUpdateDto
{
    [Range(0, 1000000, ErrorMessage = "Невалидно количество")] public int StockQuantity { get; set; }
}

// ===================== Табло и журнал =====================
public record MonthlyRevenueDto(string Month, decimal Revenue, int Orders);
public record TopProductDto(int ProductId, string Name, int Quantity, decimal Revenue);
public record TopBeekeeperDto(string ApiaryName, int Quantity, decimal Revenue);
public record StatusCountDto(string Status, int Count);

public record DashboardDto(decimal TotalRevenue, decimal RevenueLast30Days, int OrdersTotal, int OrdersLast30Days,
    int PendingOrders, decimal AverageOrderValue, int LowStockCount, int ActiveSubscriptions, int PendingApplications,
    int PendingReviews, int CustomersCount,
    List<MonthlyRevenueDto> Monthly, List<StatusCountDto> ByStatus, List<TopProductDto> TopProducts,
    List<TopBeekeeperDto> TopBeekeepers);

public record AuditLogDto(long Id, DateTime At, string UserName, string Method, string Action, string Path,
    string? EntityId, int StatusCode, string? Ip, string? Details);

public record PagedAuditDto(List<AuditLogDto> Items, int Total);
