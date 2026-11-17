using Microsoft.AspNetCore.Identity;

namespace pcheli_diplomenproekt.Services;

/// <summary>Съобщенията на ASP.NET Core Identity на български.</summary>
public class BulgarianIdentityErrorDescriber : IdentityErrorDescriber
{
    private static IdentityError E(string code, string text) => new() { Code = code, Description = text };

    public override IdentityError DefaultError() => E(nameof(DefaultError), "Възникна неизвестна грешка.");
    public override IdentityError DuplicateUserName(string userName) => E(nameof(DuplicateUserName), $"Потребителското име „{userName}“ вече е заето.");
    public override IdentityError DuplicateEmail(string email) => E(nameof(DuplicateEmail), $"Имейлът „{email}“ вече се използва.");
    public override IdentityError InvalidUserName(string? userName) => E(nameof(InvalidUserName), $"Потребителското име „{userName}“ е невалидно.");
    public override IdentityError InvalidEmail(string? email) => E(nameof(InvalidEmail), $"Имейлът „{email}“ е невалиден.");
    public override IdentityError PasswordTooShort(int length) => E(nameof(PasswordTooShort), $"Паролата трябва да е поне {length} символа.");
    public override IdentityError PasswordRequiresDigit() => E(nameof(PasswordRequiresDigit), "Паролата трябва да съдържа поне една цифра.");
    public override IdentityError PasswordRequiresLower() => E(nameof(PasswordRequiresLower), "Паролата трябва да съдържа поне една малка латинска буква.");
    public override IdentityError PasswordRequiresUpper() => E(nameof(PasswordRequiresUpper), "Паролата трябва да съдържа поне една главна латинска буква.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => E(nameof(PasswordRequiresNonAlphanumeric), "Паролата трябва да съдържа поне един специален символ.");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => E(nameof(PasswordRequiresUniqueChars), $"Паролата трябва да съдържа поне {uniqueChars} различни символа.");
    public override IdentityError PasswordMismatch() => E(nameof(PasswordMismatch), "Грешна парола.");
    public override IdentityError DuplicateRoleName(string role) => E(nameof(DuplicateRoleName), $"Ролята „{role}“ вече съществува.");
    public override IdentityError UserAlreadyInRole(string role) => E(nameof(UserAlreadyInRole), $"Потребителят вече е в роля „{role}“.");
    public override IdentityError UserLockoutNotEnabled() => E(nameof(UserLockoutNotEnabled), "Заключването не е разрешено за този потребител.");
}
