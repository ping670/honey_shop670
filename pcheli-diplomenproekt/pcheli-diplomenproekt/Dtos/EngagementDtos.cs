using System.ComponentModel.DataAnnotations;

namespace pcheli_diplomenproekt.Dtos;

// ===================== Сигурност на акаунта =====================
public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Имейлът е задължителен"), EmailAddress(ErrorMessage = "Невалиден имейл"), MaxLength(100)]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required, EmailAddress, MaxLength(100)] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(2000)] public string Token { get; set; } = string.Empty;
    [Required(ErrorMessage = "Въведете нова парола"), MaxLength(100)] public string NewPassword { get; set; } = string.Empty;
}

public class ConfirmEmailDto
{
    [Required, EmailAddress, MaxLength(100)] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(2000)] public string Token { get; set; } = string.Empty;
}

public class DeleteAccountDto
{
    [Required(ErrorMessage = "Въведете паролата си, за да потвърдите")] public string Password { get; set; } = string.Empty;
}

// ===================== Отказ на поръчка =====================
public class CancelOrderDto
{
    [Required, MaxLength(40)] public string OrderNumber { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Phone { get; set; } = string.Empty;
    [MaxLength(200)] public string? Reason { get; set; }
}

// ===================== Контакти, бюлетин, наличности =====================
public class ContactDto
{
    [Required(ErrorMessage = "Името е задължително"), MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Имейлът е задължителен"), EmailAddress(ErrorMessage = "Невалиден имейл"), MaxLength(100)] public string Email { get; set; } = string.Empty;
    [MaxLength(30)] public string? Phone { get; set; }
    [Required(ErrorMessage = "Темата е задължителна"), MaxLength(150)] public string Subject { get; set; } = string.Empty;
    [Required(ErrorMessage = "Съобщението е задължително"), MinLength(10, ErrorMessage = "Съобщението трябва да е поне 10 символа"),
     MaxLength(2000, ErrorMessage = "Съобщението може да е най-много 2000 символа")]
    public string Message { get; set; } = string.Empty;

    /// <summary>По желание – пчелар, до когото е съобщението.</summary>
    public int? BeekeeperId { get; set; }
}

public record ContactMessageDto(int Id, string Name, string Email, string? Phone, string Subject, string Message,
    int? BeekeeperId, string? ApiaryName, DateTime CreatedAt, bool IsHandled);

public class NewsletterSubscribeDto
{
    [Required(ErrorMessage = "Имейлът е задължителен"), EmailAddress(ErrorMessage = "Невалиден имейл"), MaxLength(100)]
    public string Email { get; set; } = string.Empty;
}

public class NewsletterSendDto
{
    [Required(ErrorMessage = "Темата е задължителна"), MaxLength(150)] public string Subject { get; set; } = string.Empty;
    /// <summary>Обикновен текст; абзаците се разделят с празен ред.</summary>
    [Required(ErrorMessage = "Текстът е задължителен"), MaxLength(8000)] public string Body { get; set; } = string.Empty;
}

public record NewsletterSubscriberDto(int Id, string Email, bool IsActive, DateTime CreatedAt);

public class StockAlertDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Изберете продукт")] public int ProductId { get; set; }
    [Required(ErrorMessage = "Имейлът е задължителен"), EmailAddress(ErrorMessage = "Невалиден имейл"), MaxLength(100)]
    public string Email { get; set; } = string.Empty;
}

// ===================== Администрация: пчелари, региони, потребители =====================
public class BeekeeperAdminUpsertDto
{
    [Required(ErrorMessage = "Името е задължително"), MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Името на пчелина е задължително"), MaxLength(120)] public string ApiaryName { get; set; } = string.Empty;
    [Required(ErrorMessage = "БАБХ номерът е задължителен"), MaxLength(50)] public string RegistrationNumber { get; set; } = string.Empty;
    [Required(ErrorMessage = "Изберете регион"), MaxLength(80)] public string Region { get; set; } = string.Empty;
    [Required(ErrorMessage = "Населеното място е задължително"), MaxLength(100)] public string CityOrVillage { get; set; } = string.Empty;
    [Required(ErrorMessage = "Телефонът е задължителен"), MaxLength(30)] public string Phone { get; set; } = string.Empty;
    [EmailAddress(ErrorMessage = "Невалиден имейл"), MaxLength(100)] public string? Email { get; set; }
    [MaxLength(1000)] public string Bio { get; set; } = string.Empty;
    [Range(0, 100)] public int ExperienceYears { get; set; }
    [Range(1, 100000, ErrorMessage = "Невалиден брой кошери")] public int HiveCount { get; set; } = 20;
    public bool IsVerified { get; set; } = true;
    public bool HasHoney { get; set; } = true;
    public bool HasPollen { get; set; }
    public bool IsActive { get; set; } = true;
}

public record AdminBeekeeperDto(int Id, string FullName, string ApiaryName, string RegistrationNumber, int RegionId, string Region,
    string CityOrVillage, string Phone, string Email, string Bio, int ExperienceYears, int HiveCount, bool IsVerified,
    bool HasHoney, bool HasPollen, bool IsActive, string? UserId, int ProductCount);

public class RegionUpsertDto
{
    [Required(ErrorMessage = "Името на региона е задължително"), MaxLength(80)] public string Name { get; set; } = string.Empty;
    [MaxLength(300)] public string Description { get; set; } = string.Empty;
}

public record AdminUserDto(string Id, string UserName, string Email, string FullName, string[] Roles, bool EmailConfirmed,
    bool IsLocked, DateTime CreatedAt, int OrdersCount);

public record PagedUsersDto(List<AdminUserDto> Items, int Total);

// ===================== Публична информация за магазина =====================
public record ShopInfoDto(string CompanyName, string Eik, string Address, string Email, string Phone, string VatNote);
