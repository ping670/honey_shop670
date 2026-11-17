using System.ComponentModel.DataAnnotations;

namespace pcheli_diplomenproekt.Models;

/// <summary>
/// Пчеларски регион на България (нормализиран справочник – пчеларите и заявките сочат към него с външен ключ).
/// </summary>
public class Region
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    public ICollection<Beekeeper> Beekeepers { get; set; } = new List<Beekeeper>();

    /// <summary>Фиксираните региони, сийднати с HasData (Id-тата са стабилни между миграциите).</summary>
    public static readonly Region[] Seed =
    {
        new() { Id = 1, Name = "Странджа", Description = "Вековни дъбови гори – манов мед (ЗНП)" },
        new() { Id = 2, Name = "Родопи", Description = "Високопланински билкови пасища" },
        new() { Id = 3, Name = "Тракия", Description = "Акациеви гори по поречието на Марица" },
        new() { Id = 4, Name = "Добруджа", Description = "Лавандулови и слънчогледови масиви" },
        new() { Id = 5, Name = "Дунавска равнина", Description = "Крайдунавски липови гори" },
        new() { Id = 6, Name = "Стара планина", Description = "Балкански горски букет" },
        new() { Id = 7, Name = "Пирин и Рила", Description = "Планински иглолистни и билкови масиви" },
    };
}
