using pcheli_diplomenproekt.Dtos;

namespace pcheli_diplomenproekt.Services;

/// <summary>Данните на търговеца за правните страници и разписките – задават се в конфигурацията (секция „Shop“).</summary>
public static class ShopInfo
{
    public static ShopInfoDto From(IConfiguration c) => new(
        c["Shop:CompanyName"] ?? "Пчелари България",
        c["Shop:CompanyEik"] ?? string.Empty,
        c["Shop:CompanyAddress"] ?? string.Empty,
        c["Shop:ContactEmail"] ?? c["Admin:Email"] ?? string.Empty,
        c["Shop:SenderPhone"] ?? "0888 123 456",
        c["Shop:VatNote"] ?? string.Empty);
}
