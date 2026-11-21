using pcheli_diplomenproekt.Dtos;

namespace pcheli_diplomenproekt.Services;

public interface IShippingService
{
    IReadOnlyList<CourierOptionDto> Couriers { get; }
    decimal FreeShippingFrom { get; }
    string NormalizeCourier(string? courier);
    string NormalizeDeliveryType(string? type);

    /// <summary>Цена на доставката в евро. Над прага за безплатна доставка е 0.</summary>
    decimal Calculate(string courier, string deliveryType, decimal weightKg, decimal netAmount);
}

/// <summary>
/// Калкулатор на доставката за Еконт и Спиди по тарифна таблица (цена до включено тегло + надбавка за всеки следващ кг).
/// Тарифите се задават в appsettings.json („Shipping“); без конфигурация се ползват стойностите по подразбиране.
/// Това е приблизителна оценка, а не заявка към системите на куриерите.
/// </summary>
public class ShippingService : IShippingService
{
    public const string Office = "Офис";
    public const string Address = "Адрес";
    public const string DefaultCourier = "Еконт";

    private readonly Dictionary<string, (decimal Office, decimal Address)> _tariffs;
    private readonly decimal _includedKg;
    private readonly decimal _extraKgPrice;

    public decimal FreeShippingFrom { get; }
    public IReadOnlyList<CourierOptionDto> Couriers { get; }

    public ShippingService(Microsoft.Extensions.Configuration.IConfiguration? config = null)
    {
        var s = config?.GetSection("Shipping");
        FreeShippingFrom = s?.GetValue<decimal?>("FreeShippingFrom") ?? 25.00m;
        _includedKg = s?.GetValue<decimal?>("IncludedKg") ?? 2m;
        _extraKgPrice = s?.GetValue<decimal?>("ExtraKgPrice") ?? 0.50m;

        _tariffs = new Dictionary<string, (decimal, decimal)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Еконт"] = (3.00m, 4.20m),
            ["Спиди"] = (2.90m, 4.00m)
        };
        foreach (var c in s?.GetSection("Couriers").GetChildren() ?? Enumerable.Empty<Microsoft.Extensions.Configuration.IConfigurationSection>())
        {
            var office = c.GetValue<decimal?>("OfficePrice");
            var address = c.GetValue<decimal?>("AddressPrice");
            if (office.HasValue && address.HasValue) _tariffs[c.Key] = (office.Value, address.Value);
        }

        Couriers = _tariffs.Select(t => new CourierOptionDto(t.Key, t.Value.Office, t.Value.Address)).ToList();
    }

    public string NormalizeCourier(string? courier)
    {
        if (string.IsNullOrWhiteSpace(courier)) return DefaultCourier;
        var match = _tariffs.Keys.FirstOrDefault(k => string.Equals(k, courier.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? throw new BusinessException("Невалиден куриер. Изберете: " + string.Join(", ", _tariffs.Keys) + ".");
    }

    public string NormalizeDeliveryType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return Office;
        if (string.Equals(type.Trim(), Office, StringComparison.OrdinalIgnoreCase)) return Office;
        if (string.Equals(type.Trim(), Address, StringComparison.OrdinalIgnoreCase)) return Address;
        throw new BusinessException("Невалиден тип доставка. Изберете „Офис“ или „Адрес“.");
    }

    public decimal Calculate(string courier, string deliveryType, decimal weightKg, decimal netAmount)
    {
        if (netAmount >= FreeShippingFrom) return 0m;
        var t = _tariffs[NormalizeCourier(courier)];
        var basePrice = NormalizeDeliveryType(deliveryType) == Address ? t.Address : t.Office;
        var extraKg = Math.Max(0m, Math.Ceiling(weightKg - _includedKg));
        return basePrice + extraKg * _extraKgPrice;
    }
}
