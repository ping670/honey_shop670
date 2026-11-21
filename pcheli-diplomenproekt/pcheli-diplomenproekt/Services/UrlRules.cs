namespace pcheli_diplomenproekt.Services;

/// <summary>Проверка на адреси, въведени от потребители (снимки, лабораторни протоколи) – само безопасни схеми.</summary>
public static class UrlRules
{
    public static bool IsSafe(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        var u = url.Trim();
        if (u.Length > 300 || u.Any(char.IsControl)) return false;
        // относителни пътища към нашия сайт или абсолютни http(s) адреси; „//host“ и „javascript:“ не са позволени
        if (u.StartsWith("//")) return false;
        if (u.StartsWith('/') || u.StartsWith("./") || u.StartsWith("images/")) return !u.Contains("..");
        return u.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               u.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
    }

    public static void Require(string? url, string field)
    {
        if (!IsSafe(url))
            throw new BusinessException($"Невалиден адрес в полето „{field}“. Позволени са пътища от сайта или http(s) адреси.");
    }
}
