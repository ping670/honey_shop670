using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Data;

/// <summary>Начални данни (пчелари, продукти с опаковки). Генерирани от оригиналния каталог.</summary>
public static class SeedData
{
    public static List<Beekeeper> Beekeepers() => new()
    {
        new() { FullName = "Иван Стоянов", ApiaryName = "Пчелин «Странджанска Дъбрава»", RegistrationNumber = "БАБХ-4001-0812", RegionId = 1, CityOrVillage = "гр. Малко Търново", Phone = "0888 123 456", Bio = "Трети потомствен пчелар в района на Малко Търново. Кошерите са разположени в чисти дъбови гори. Добиваме автентичен странджански манов мед (ЗНП) и горски прашец.", ExperienceYears = 24, HiveCount = 180, Rating = 5, IsVerified = true, HasHoney = true, HasPollen = true },
        new() { FullName = "Димитър Родопски", ApiaryName = "Био Пчелин «Родопска Панорама»", RegistrationNumber = "БАБХ-4700-0234", RegionId = 2, CityOrVillage = "с. Момчиловци (1250 м надм. вис.)", Phone = "0887 654 321", Bio = "Пчелинът се намира във високопланинския пояс на Родопите сред пасища с мащерка, риган и жълт кантарион. Предлагаме планински букет и пресен замразен прашец.", ExperienceYears = 18, HiveCount = 140, Rating = 4.95, IsVerified = true, HasHoney = true, HasPollen = true },
        new() { FullName = "Христо Маринов", ApiaryName = "Семеен Пчелин «Златна Добруджа»", RegistrationNumber = "БАБХ-9300-0419", RegionId = 4, CityOrVillage = "гр. Генерал Тошево", Phone = "0885 998 877", Bio = "Семейно пчеларство с над 200 кошера в района на Генерал Тошево. Добиваме чист лавандулов мед и слънчогледов прашец с високо съдържание на биофлавоноиди.", ExperienceYears = 15, HiveCount = 210, Rating = 4.9, IsVerified = true, HasHoney = true, HasPollen = true },
        new() { FullName = "Стоян Петков", ApiaryName = "Пчелин «Тракийска Долина»", RegistrationNumber = "БАБХ-4000-0551", RegionId = 3, CityOrVillage = "гр. Садово / Пловдив", Phone = "0894 332 211", Bio = "Пчелинът е разположен близо до поречието на Марица сред богати акациеви гори. Произвеждаме прозрачен акациев мед и пролетен цветен прашец.", ExperienceYears = 12, HiveCount = 120, Rating = 4.9, IsVerified = true, HasHoney = true, HasPollen = true },
        new() { FullName = "Николай Василев", ApiaryName = "Пчелин «Балканско Ехо»", RegistrationNumber = "БАБХ-5000-0112", RegionId = 6, CityOrVillage = "гр. Трявна", Phone = "0876 114 455", Bio = "Стационарен пчелин в Тревненския балкан. Пашата е от дива детелина, къпина, малина и планински билки. Предлагаме мед букет и чист пчелен прашец.", ExperienceYears = 20, HiveCount = 95, Rating = 4.95, IsVerified = true, HasHoney = true, HasPollen = true },
        new() { FullName = "Красимир Георгиев", ApiaryName = "Пчеларска ферма «Дунавски Бряг»", RegistrationNumber = "БАБХ-7000-0388", RegionId = 5, CityOrVillage = "гр. Русе", Phone = "0883 227 788", Bio = "Пчеларство по поречието на Дунав в близост до липовите масиви на Русенски Лом. Чист монофлорен липов мед и полифлорен цветен прашец.", ExperienceYears = 16, HiveCount = 160, Rating = 4.88, IsVerified = true, HasHoney = true, HasPollen = true },
    };

    /// <param name="beekeepers">Вече записаните пчелари; индексът отговаря на beekeeperId-1 от оригиналния каталог.</param>
    public static List<Product> Products(IReadOnlyList<Beekeeper> beekeepers) => new()
    {
        new()
        {
            BeekeeperId = beekeepers[0].Id,
            Name = "Странджански Манов Мед (ЗНП)", Type = "Мед", Category = "manov", Origin = "Странджа (Защитено Наименование)",
            Tag = "ЗНП Сертификат", Image = "./images/manov.jpg", Description = "Тъмен манов мед, добит от вековните дъбови гори в Странджа. Богат на калий, желязо и минерални соли. Кристализира изключително бавно.",
            Color = "Тъмно кехлибарен", Aroma = "Дъбова мана и гора", Crystallization = "Бавна (над 2 години)",
            HarvestDate = "Реколта Август 2026", Purity = "100% Чист добив, без захари", TastingNotes = "Плътен, наситен вкус с леки карамелени и смолисти нотки.",
            Rating = 5, QuizTags = "antioxidant,immunity,dark",
            Variants = new List<ProductVariant>
            {
                new() { Label = "Буркан 0.9 кг", Weight = "0.9kg", Price = 12.27m },
                new() { Label = "Буркан 0.5 кг", Weight = "0.5kg", Price = 7.16m },
            }
        },
        new()
        {
            BeekeeperId = beekeepers[0].Id,
            Name = "Натурален Пчелен Прашец от Странджа", Type = "Пчелен прашец", Category = "pollen", Origin = "Странджа (Малко Търново)",
            Tag = "Сушен цветен прашец", Image = "./images/pollen.jpg", Description = "Сушен цветен прашец от дивите ливади и гори на Странджа. Пълен източник на растителен протеин, аминокиселини и биофлавоноиди. Подходящ за ежедневен прием сутрин.",
            Color = "Пъстри гранули", Aroma = "Цветен и смолист", Crystallization = "Сушени гранули",
            HarvestDate = "Реколта Юли 2026", Purity = "100% Натурален сбор", TastingNotes = "Приятен цветен аромат с леко орехов послевкус.",
            Rating = 4.95, QuizTags = "immunity,antioxidant",
            Variants = new List<ProductVariant>
            {
                new() { Label = "Буркан 250 г", Weight = "0.25kg", Price = 8.5m },
                new() { Label = "Пакет 500 г", Weight = "0.5kg", Price = 16m },
            }
        },
        new()
        {
            BeekeeperId = beekeepers[3].Id,
            Name = "Тракийски Акациев Мед", Type = "Мед", Category = "acacia", Origin = "Тракия (Садово / Пловдив)",
            Tag = "Фин & Мек", Image = "./images/acacia.jpg", Description = "Светъл и прозрачен акациев мед от горите край Марица. Изключително нежен вкус без натрапчива сладост. Един от най-подходящите медове за деца.",
            Color = "Светло сламеножълт", Aroma = "Нежен акациев цвят", Crystallization = "Много бавна",
            HarvestDate = "Реколта Май 2026", Purity = "Чиста бяла акация", TastingNotes = "Ефирен, мек и сладък вкус без киселинност.",
            Rating = 4.9, QuizTags = "tea,kids,light",
            Variants = new List<ProductVariant>
            {
                new() { Label = "Буркан 0.9 кг", Weight = "0.9kg", Price = 10.74m },
                new() { Label = "Буркан 0.5 кг", Weight = "0.5kg", Price = 6.39m },
            }
        },
        new()
        {
            BeekeeperId = beekeepers[1].Id,
            Name = "Родопски Планински Билков Букет", Type = "Мед", Category = "herbal", Origin = "Родопи (Момчиловци, 1250 м)",
            Tag = "Планински билки", Image = "./images/rhodope.jpg", Description = "Добит от диви планински билки над 1200 м надморска височина — мащерка, риган, бяла детелина и жълт кантарион. Ароматен и богат на естествени антиоксиданти.",
            Color = "Кехлибарен", Aroma = "Дива планинска мащерка", Crystallization = "Средна естествена",
            HarvestDate = "Реколта Юли 2026", Purity = "Диворастящи лечебни билки", TastingNotes = "Многопластов билков вкус с отчетлив аромат на мащерка.",
            Rating = 4.95, QuizTags = "cold,immunity,herbal",
            Variants = new List<ProductVariant>
            {
                new() { Label = "Буркан 0.9 кг", Weight = "0.9kg", Price = 9.97m },
                new() { Label = "Буркан 0.5 кг", Weight = "0.5kg", Price = 5.88m },
            }
        },
        new()
        {
            BeekeeperId = beekeepers[1].Id,
            Name = "Пресен Суров Замразен Пчелен Прашец", Type = "Пчелен прашец", Category = "pollen", Origin = "Родопи (Високопланински пояс)",
            Tag = "Суров замразен (-18°C)", Image = "./images/pollen.jpg", Description = "Шоково замразен веднага след събиране от кошера при -18°C. Без сушене и загряване, което запазва 100% от живите ензими, пробиотици и витамини.",
            Color = "Ярко жълто и оранжево", Aroma = "Свеж цвят и цитрус", Crystallization = "Суров замразен",
            HarvestDate = "Реколта Юни 2026", Purity = "100% Суров жив прашец", TastingNotes = "Разтапя се леко на езика с приятен свеж цветен вкус.",
            Rating = 5, QuizTags = "immunity,antioxidant",
            Variants = new List<ProductVariant>
            {
                new() { Label = "Кутия 300 г (Замразен)", Weight = "0.3kg", Price = 11.2m },
                new() { Label = "Кутия 600 г (Замразен)", Weight = "0.6kg", Price = 21m },
            }
        },
        new()
        {
            BeekeeperId = beekeepers[2].Id,
            Name = "Добруджански Лавандулов Мед", Type = "Мед", Category = "lavender", Origin = "Добруджа (Генерал Тошево)",
            Tag = "Лавандулов цвят", Image = "./images/rhodope.jpg", Description = "Чист монофлорен мед от лавандуловите масиви на Добруджа. Отличава се с фин аромат и успокояващо действие. Чудесен за топъл билков чай вечер.",
            Color = "Светъл млечен кехлибар", Aroma = "Лавандулов нюанс", Crystallization = "Фина кремообразна",
            HarvestDate = "Реколта Юни 2026", Purity = "Добруджанска лавандула", TastingNotes = "Кадифен вкус с деликатен лавандулов послевкус.",
            Rating = 4.9, QuizTags = "tea,herbal,light",
            Variants = new List<ProductVariant>
            {
                new() { Label = "Буркан 0.9 кг", Weight = "0.9kg", Price = 10.5m },
                new() { Label = "Буркан 0.5 кг", Weight = "0.5kg", Price = 6.2m },
            }
        },
        new()
        {
            BeekeeperId = beekeepers[2].Id,
            Name = "Златен Слънчогледов Пчелен Прашец", Type = "Пчелен прашец", Category = "pollen", Origin = "Добруджа (Генерал Тошево)",
            Tag = "Слънчогледов прашец", Image = "./images/pollen.jpg", Description = "Златист прашец от обширните слънчогледови масиви на Добруджа. Отличен природен източник на рутин и лецитин за тонус и подкрепа на кръвообращението.",
            Color = "Ярко златисто-жълт", Aroma = "Сладък слънчоглед", Crystallization = "Сушени гранули",
            HarvestDate = "Реколта Август 2026", Purity = "Чист слънчогледов сбор", TastingNotes = "Сладникав вкус с леки маслени нотки.",
            Rating = 4.85, QuizTags = "immunity,tea",
            Variants = new List<ProductVariant>
            {
                new() { Label = "Буркан 250 г", Weight = "0.25kg", Price = 7.9m },
            }
        },
        new()
        {
            BeekeeperId = beekeepers[5].Id,
            Name = "Крайдунавски Липов Мед", Type = "Мед", Category = "linden", Origin = "Дунавска равнина (гр. Русе)",
            Tag = "Ароматна липа", Image = "./images/acacia.jpg", Description = "Наситен монофлорен мед от вековните крайдунавски липови гори край Русе. Традиционно домашно средство при настинка, болки в гърлото и грип.",
            Color = "Жълто-златист", Aroma = "Уханен липов цвят", Crystallization = "Средна кристализация",
            HarvestDate = "Реколта Юни 2026", Purity = "Сребролистна дунавска липа", TastingNotes = "Свеж, затоплящ вкус с отчетлив аромат на цъфнала липа.",
            Rating = 4.88, QuizTags = "cold,herbal",
            Variants = new List<ProductVariant>
            {
                new() { Label = "Буркан 0.9 кг", Weight = "0.9kg", Price = 9.8m },
                new() { Label = "Буркан 0.5 кг", Weight = "0.5kg", Price = 5.7m },
            }
        },
        new()
        {
            BeekeeperId = beekeepers[4].Id,
            Name = "Балкански Горски Букет Мед", Type = "Мед", Category = "herbal", Origin = "Стара планина (гр. Трявна)",
            Tag = "Горски букет", Image = "./images/rhodope.jpg", Description = "Многоцветен горски мед от девствените планински масиви около Трявна. Пашата е от дива детелина, къпина, малина и горски билки.",
            Color = "Тъмно кехлибарен", Aroma = "Горски билки и листа", Crystallization = "Естествена средна",
            HarvestDate = "Реколта Юли 2026", Purity = "Балкански горски пасища", TastingNotes = "Балансиран, дълбок и плътен горски вкус.",
            Rating = 4.95, QuizTags = "immunity,herbal",
            Variants = new List<ProductVariant>
            {
                new() { Label = "Буркан 0.9 кг", Weight = "0.9kg", Price = 10.1m },
                new() { Label = "Буркан 0.5 кг", Weight = "0.5kg", Price = 5.95m },
            }
        },
    };
}
