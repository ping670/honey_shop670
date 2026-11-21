using Microsoft.EntityFrameworkCore;
using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Data;

/// <summary>Допълнителни начални данни: английски текстове, произход на партидите, подаръчни комплекти и статии за блога.</summary>
public static class SeedExtras
{
    private static readonly Dictionary<string, (string Name, string Description, string Batch)> English = new()
    {
        ["Странджански Манов Мед (ЗНП)"] = ("Strandzha Honeydew Honey (PDO)", "Dark honeydew honey from the ancient oak forests of Strandzha. Rich in potassium, iron and minerals. Crystallises very slowly.", "STR-2026-08"),
        ["Натурален Пчелен Прашец от Странджа"] = ("Natural Bee Pollen from Strandzha", "Dried multifloral pollen from the wild meadows and forests of Strandzha. A source of plant protein and amino acids, best taken in the morning.", "STR-P-2026-07"),
        ["Тракийски Акациев Мед"] = ("Thracian Acacia Honey", "Light, clear acacia honey from the forests along the Maritsa river. Very delicate taste without a heavy sweetness - a favourite for children and tea.", "TRK-2026-05"),
        ["Родопски Планински Билков Букет"] = ("Rhodope Mountain Herbal Bouquet Honey", "Made from wild mountain herbs above 1200 m - thyme, oregano, white clover and St John's wort. Aromatic and rich in natural antioxidants.", "ROD-2026-07"),
        ["Пресен Суров Замразен Пчелен Прашец"] = ("Fresh Raw Frozen Bee Pollen", "Flash-frozen at -18 C right after collection. No drying or heating, so enzymes and vitamins are preserved.", "ROD-P-2026-06"),
        ["Добруджански Лавандулов Мед"] = ("Dobrudja Lavender Honey", "Pure monofloral honey from the lavender fields of Dobrudja. Fine aroma, lovely in an evening herbal tea.", "DOB-2026-06"),
        ["Златен Слънчогледов Пчелен Прашец"] = ("Golden Sunflower Bee Pollen", "Golden pollen from the sunflower fields of Dobrudja - a natural source of rutin and lecithin.", "DOB-P-2026-08"),
        ["Крайдунавски Липов Мед"] = ("Danube Linden Honey", "Full-bodied monofloral honey from the old linden forests along the Danube near Ruse. A traditional home remedy for cold-season sore throats.", "DUN-2026-06"),
        ["Балкански Горски Букет Мед"] = ("Balkan Forest Bouquet Honey", "Multifloral forest honey from the mountains around Tryavna - wild clover, blackberry, raspberry and forest herbs.", "BAL-2026-07")
    };

    /// <summary>Идемпотентно допълва произхода и английските текстове на каталожните продукти (само празните полета).</summary>
    public static async Task EnrichProductsAsync(ApplicationDbContext db)
    {
        var products = await db.Products.Include(p => p.Beekeeper).ToListAsync();
        var changed = false;
        foreach (var p in products)
        {
            if (English.TryGetValue(p.Name, out var en))
            {
                if (p.NameEn.Length == 0) { p.NameEn = en.Name; changed = true; }
                if (p.DescriptionEn.Length == 0) { p.DescriptionEn = en.Description; changed = true; }
                if (p.BatchNumber.Length == 0) { p.BatchNumber = en.Batch; p.HarvestYear ??= 2026; changed = true; }
            }
            if (p.Certificates.Length == 0 && p.Beekeeper != null)
            {
                p.Certificates = $"Регистрация на обекта по БАБХ: {p.Beekeeper.RegistrationNumber}" +
                                 (p.Category == "manov" ? "; Защитено наименование за произход (ЗНП)" : "");
                changed = true;
            }
        }
        if (changed) await db.SaveChangesAsync();
    }

    public static async Task SeedGiftSetsAsync(ApplicationDbContext db)
    {
        if (await db.Products.AnyAsync(p => p.Type == "Подаръчен комплект")) return;
        var host = await db.Beekeepers.OrderBy(b => b.Id).Skip(3).FirstOrDefaultAsync() ?? await db.Beekeepers.OrderBy(b => b.Id).FirstOrDefaultAsync();
        if (host is null) return;

        db.Products.AddRange(
            new Product
            {
                BeekeeperId = host.Id, Name = "Подаръчен комплект «Три медни вкуса»", Type = "Подаръчен комплект", Category = "gift",
                Origin = "България (селекция от пчелини)", Tag = "Идеален подарък", Image = "./images/rhodope.jpg",
                Description = "Три бурканчета мед с различен характер в подаръчна опаковка – за ценители и за хора, които искат да опитат повече от един вкус.",
                Contents = "Акациев мед 0.5 кг; Липов мед 0.5 кг; Билков букет 0.5 кг; дървена лъжичка за мед",
                HarvestDate = "Реколта 2026", Purity = "100% натурален мед", TastingNotes = "Нежен акациев, свеж липов и ароматен билков вкус.",
                NameEn = "Gift Set \"Three Honey Flavours\"", DescriptionEn = "Three jars of honey with different characters in a gift box - acacia, linden and herbal bouquet, plus a wooden honey spoon.",
                Rating = 5, QuizTags = "kids,tea,herbal",
                Variants = new List<ProductVariant> { new() { Label = "Комплект 3 × 0.5 кг", Weight = "1.5kg", Price = 19.90m, StockQuantity = 40 } }
            },
            new Product
            {
                BeekeeperId = host.Id, Name = "Подаръчен комплект «Пчелна закуска»", Type = "Подаръчен комплект", Category = "gift",
                Origin = "България (селекция от пчелини)", Tag = "Подарък", Image = "./images/acacia.jpg",
                Description = "Всичко за здравословна закуска: нежен акациев мед и пчелен прашец, красиво опаковани за подарък.",
                Contents = "Акациев мед 0.5 кг; Пчелен прашец 250 г; дървен черпак за мед",
                HarvestDate = "Реколта 2026", Purity = "100% натурални продукти", TastingNotes = "Мек мед и цветен прашец за каша, кисело мляко и смутита.",
                NameEn = "Gift Set \"Bee Breakfast\"", DescriptionEn = "Everything for a wholesome breakfast: delicate acacia honey and bee pollen in a gift box, with a wooden dipper.",
                Rating = 5, QuizTags = "kids,immunity,light",
                Variants = new List<ProductVariant> { new() { Label = "Комплект закуска", Weight = "0.9kg", Price = 16.90m, StockQuantity = 40 } }
            });
        await db.SaveChangesAsync();
    }

    public static async Task SeedArticlesAsync(ApplicationDbContext db)
    {
        if (await db.Articles.AnyAsync()) return;
        var now = DateTime.UtcNow;

        db.Articles.AddRange(
            new Article
            {
                Slug = "kak-da-razpoznaem-istinskiya-med", Title = "Как да разпознаем истинския мед", Category = "Съвети",
                Summary = "Кристализацията не е признак за фалшив мед. Ето на какво наистина да обърнете внимание при покупка.",
                CoverImage = "./images/acacia.jpg", PublishedAt = now.AddDays(-10),
                Content = """
                    Истинският мед се купува най-сигурно от човека, който го произвежда. Затова в платформата виждате името на пчеларя, региона и регистрационния му номер по БАБХ.

                    ## Кристализацията е нормална
                    Почти всеки натурален мед рано или късно кристализира. Скоростта зависи от съотношението на глюкоза и фруктоза – акациевият мед се запазва течен с месеци, а рапичният се сгъстява за седмици. Течният мед в магазина през зимата по-често е знак за загряване, отколкото за „по-хубав“ мед.

                    ## На какво да обърнете внимание
                    - Произход: пчелар с регистриран пчелин и ясно посочен регион.
                    - Партида и година на реколтата, написани на буркана.
                    - Цената: твърде евтин мед често е смесен или подхранен със захарен сироп.
                    - Вкус и аромат, типични за посочения вид – акацията е нежна, мановият мед е тъмен и наситен.

                    ## Домашните „тестове“ не са надеждни
                    Капка във вода, огън с фитил или йод върху меда не доказват нищо категорично. Достоверна проверка се прави само в лаборатория – показатели като съдържание на вода, HMF и диастазно число показват свежест и дали медът е загряван.

                    ## Нашият съвет
                    Купувайте от пчелари, които показват партида, година на реколтата и лабораторен протокол, и задавайте въпроси директно – телефонът на всеки пчелар е в профила му.
                    """
            },
            new Article
            {
                Slug = "polzi-ot-pchelniya-med-i-kak-da-go-izpolzvame", Title = "Пчелният мед: как да го използваме разумно", Category = "Здраве",
                Summary = "Какво представлява медът, колко е разумно да се яде и кога да го избягваме.",
                CoverImage = "./images/manov.jpg", PublishedAt = now.AddDays(-6),
                Content = """
                    Медът е натурален подсладител с богат състав – освен захари съдържа малки количества минерали, органични киселини и антиоксидантни съединения. Тъмните видове, като мановия мед, обикновено имат повече минерали.

                    ## Колко и как
                    Медът си остава захар, затова е най-добре да се консумира умерено – една до две супени лъжици на ден като заместител на рафинираната захар. При диабет или други заболявания се посъветвайте с лекар.

                    ## Не го загрявайте силно
                    При високи температури се губят част от ензимите и ароматът. Добавяйте меда в чая, когато е вече хладък, а не в кипяща вода.

                    ## Кога да го избягваме
                    - Не давайте мед на деца под 1 годишна възраст – има риск от инфантилен ботулизъм.
                    - При алергия към пчелни продукти или полени първо се консултирайте с лекар.

                    ## Съхранение
                    Пазете меда в плътно затворен буркан на сухо място, далеч от директна слънчева светлина. Хладилник не е нужен – там кристализира по-бързо.

                    Статията е с информационна цел и не замества съвет от лекар.
                    """
            },
            new Article
            {
                Slug = "retsepti-s-med-tri-lesni-idei", Title = "Рецепти с мед: три лесни идеи", Category = "Рецепти",
                Summary = "Билков чай, дресинг за салата и каша с мед и пчелен прашец – за по-малко от 10 минути.",
                CoverImage = "./images/rhodope.jpg", PublishedAt = now.AddDays(-2),
                Content = """
                    Три бързи идеи, с които да използвате меда по-интересно от лъжица в чая.

                    ## 1. Билков чай с мед и лимон
                    Запарете билка по избор (липа, мащерка или лайка), оставете чая да изстине до приятно топъл и добавете една лъжица мед и резен лимон. Така запазвате аромата на меда.

                    ## 2. Дресинг за салата с мед и горчица
                    - 3 с.л. зехтин
                    - 1 с.л. ябълков оцет или лимонов сок
                    - 1 ч.л. горчица
                    - 1 ч.л. мед (акациев или липов)
                    - сол и пипер на вкус
                    Разбийте всичко в малък буркан. Подходящ за зелени салати и печени зеленчуци.

                    ## 3. Овесена каша с мед и пчелен прашец
                    Сварете овесени ядки с мляко, оставете ги да поизстинат, добавете лъжица мед и поръсете с чаена лъжичка пчелен прашец. Прашецът не се загрява, за да запази хранителните си свойства.

                    Приятен апетит!
                    """
            });
        await db.SaveChangesAsync();
    }
}
