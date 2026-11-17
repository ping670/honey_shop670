using System.ComponentModel.DataAnnotations;

namespace pcheli_diplomenproekt.Models;

public class PromoCode
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    [Range(1, 100)]
    public int DiscountPercent { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? ValidUntil { get; set; }
}
