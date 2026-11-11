using pcheli_diplomenproekt.Models;

namespace pcheli_diplomenproekt.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        // Създаване на базата данни ако не съществува
        context.Database.EnsureCreated();

        if (context.Beekeepers.Any())
        {
            return; // Вече е захранена
        }

        var beekeepers = new List<Beekeeper>
        {
            new()
            {
                FullName = "Иван Стоянов",
                ApiaryName = "Пчелин «Странджанска Дъбрава»",
                RegistrationNumber = "БАБХ-4001-0812",
                Region = "Странджа",
                CityOrVillage = "гр. Малко Търново",
                Phone = "+359 88 812 3456",
                Email = "ivan.strandja@pchelaribg.com",
                Bio = "Трети потомствен пчелар в сърцето на природен парк Странджа. Произвеждаме световноизвестния странджански манов мед и свеж планински цветен прашец.",
                ExperienceYears = 24,
                HiveCount = 180,
                Rating = 5.0,
                IsVerified = true,
                HasHoney = true,
                HasPollen = true
            },
            new()
            {
                FullName = "Димитър Родопски",
                ApiaryName = "Био Пчелин «Родопска Панорама»",
                RegistrationNumber = "БАБХ-4700-0234",
                Region = "Родопи",
                CityOrVillage = "с. Момчиловци, общ. Смолян",
                Phone = "+359 88 765 4321",
                Email = "dimitar.rhodope@pchelaribg.com",
                Bio = "Кошерите ни са разположени на 1250 м надморска височина сред чисти борови гори и алпийски ливади, богати на мащерка, жълт кантарион и риган.",
                ExperienceYears = 18,
                HiveCount = 140,
                Rating = 4.95,
                IsVerified = true,
                HasHoney = true,
                HasPollen = true
            },
            new()
            {
                FullName = "Христо Маринов",
                ApiaryName = "Семеен Пчелин «Златна Добруджа»",
                RegistrationNumber = "БАБХ-9300-0419",
                Region = "Добруджа",
                CityOrVillage = "гр. Генерал Тошево",
                Phone = "+359 88 599 8877",
                Email = "hristo.dobrudja@pchelaribg.com",
                Bio = "Произвеждаме първокласен лавандулов и слънчогледов мед, както и чист цветен пчелен прашец от златните поля на Добруджа.",
                ExperienceYears = 15,
                HiveCount = 210,
                Rating = 4.9,
                IsVerified = true,
                HasHoney = true,
                HasPollen = true
            },
            new()
            {
                FullName = "Стоян Петков",
                ApiaryName = "Пчелин «Тракийска Долина»",
                RegistrationNumber = "БАБХ-4000-0551",
                Region = "Тракия",
                CityOrVillage = "гр. Садово / Пловдив",
                Phone = "+359 89 433 2211",
                Email = "stoyan.trakiya@pchelaribg.com",
                Bio = "Разположен сред огромните акациеви масиви на Тракия. Натурален прозрачен акациев мед и пресен витаминозен пчелен прашец.",
                ExperienceYears = 12,
                HiveCount = 120,
                Rating = 4.9,
                IsVerified = true,
                HasHoney = true,
                HasPollen = true
            },
            new()
            {
                FullName = "Николай Василев",
                ApiaryName = "Пчелин «Балканско Ехо»",
                RegistrationNumber = "БАБХ-5000-0112",
                Region = "Стара планина",
                CityOrVillage = "гр. Трявна",
                Phone = "+359 87 611 4455",
                Email = "nikolay.balkan@pchelaribg.com",
                Bio = "Екологично пчеларство в Еленския и Тревненския балкан. Диворастящи горски билки, малина и къпина.",
                ExperienceYears = 20,
                HiveCount = 95,
                Rating = 4.95,
                IsVerified = true,
                HasHoney = true,
                HasPollen = true
            },
            new()
            {
                FullName = "Красимир Георгиев",
                ApiaryName = "Пчеларска ферма «Дунавски Бряг»",
                RegistrationNumber = "БАБХ-7000-0388",
                Region = "Дунавска равнина",
                CityOrVillage = "гр. Русе (с. Иваново)",
                Phone = "+359 88 322 7788",
                Email = "krasimir.danube@pchelaribg.com",
                Bio = "Добив на неповторим липов мед от вековните крайдунавски липови гори и богат полифлорен прашец.",
                ExperienceYears = 16,
                HiveCount = 160,
                Rating = 4.88,
                IsVerified = true,
                HasHoney = true,
                HasPollen = true
            }
        };

        context.Beekeepers.AddRange(beekeepers);
        context.SaveChanges();

        var bStrandja = beekeepers[0];
        var bRhodope = beekeepers[1];
        var bDobrudja = beekeepers[2];
        var bTrakiya = beekeepers[3];
        var bBalkan = beekeepers[4];
        var bDanube = beekeepers[5];

        var products = new List<Product>
        {
            // --- СТРАНДЖА ---
            new()
            {
                BeekeeperId = bStrandja.Id,
                Name = "Странджански Манов Мед (ЗНП Сертификат)",
                Type = "Мед",
                Category = "manov",
                Origin = "Странджа (гр. Малко Търново)",
                Description = "Световно признат тъмен манов мед, добит от вековните дъбови гори на Странджа. Богат на минерали, калий, магнезий и антиоксиданти.",
                HarvestDate = "Реколта Август 2026",
                Purity = "96.4% Манов индекс",
                TastingNotes = "Наситен, плътен карамелено-дъбов вкус с деликатна тръпчивост.",
                Price = 12.27m,
                Weight = "0.9 кг",
                Image = "./images/manov.jpg",
                InStock = true,
                Rating = 5.0,
                Tag = "ЗНП Сертифициран"
            },
            new()
            {
                BeekeeperId = bStrandja.Id,
                Name = "Натурален Пчелен Прашец от Странджа",
                Type = "Пчелен прашец",
                Category = "pollen_dried",
                Origin = "Странджа (Манастирски дъбрави)",
                Description = "Чист сушен пчелен прашец от горски билки и дъбова мана. Изключително силен природен биостимулатор, богат на всички 22 незаменими аминокиселини.",
                HarvestDate = "Реколта Юли 2026",
                Purity = "100% Натурален горски прашец",
                TastingNotes = "Мек цветен аромат с леко сладък и смолист послевкус.",
                Price = 8.50m,
                Weight = "250 г",
                Image = "./images/pollen.jpg",
                InStock = true,
                Rating = 4.95,
                Tag = "Суперхрана"
            },

            // --- РОДОПИ ---
            new()
            {
                BeekeeperId = bRhodope.Id,
                Name = "Родопски Планински Билков Букет",
                Type = "Мед",
                Category = "herbal",
                Origin = "Родопи (с. Момчиловци, 1250м)",
                Description = "Добит от над 400 вида диворастящи билки, мащерка, риган и планинска детелина в кристално чистия Родопски въздух.",
                HarvestDate = "Реколта Юли 2026",
                Purity = "94.8% Планински билки",
                TastingNotes = "Богат, многослоен билков профил с нотки на мащерка и горска малина.",
                Price = 9.97m,
                Weight = "0.9 кг",
                Image = "./images/rhodope.jpg",
                InStock = true,
                Rating = 4.95,
                Tag = "Био Производство"
            },
            new()
            {
                BeekeeperId = bRhodope.Id,
                Name = "Пресен Суров Замразен Пчелен Прашец от Родопите",
                Type = "Пчелен прашец",
                Category = "pollen_fresh",
                Origin = "Родопи (Високопланински пояс)",
                Description = "Шоково замразен веднага след събиране от кошера без топлинна обработка, за да запази 100% от живите ензими, витамини C, B комплекс и минерали.",
                HarvestDate = "Реколта Юни 2026",
                Purity = "100% Суров жив прашец",
                TastingNotes = "Разтапящ се на езика, свеж цветен и цитрусов аромат.",
                Price = 11.20m,
                Weight = "300 г",
                Image = "./images/pollen.jpg",
                InStock = true,
                Rating = 5.0,
                Tag = "Суров & Жив"
            },

            // --- ДОБРУДЖА ---
            new()
            {
                BeekeeperId = bDobrudja.Id,
                Name = "Добруджански Лавандулов Мед",
                Type = "Мед",
                Category = "lavender",
                Origin = "Добруджа (Генерал Тошево)",
                Description = "Уникален ароматен монофлорен мед от лавандуловите полета на Добруджа. Известен със своето успокояващо действие при безсъние и стрес.",
                HarvestDate = "Реколта Юни 2026",
                Purity = "91.2% Лавандулов цвят",
                TastingNotes = "Фин кадифен вкус с неподражаем лилав лавандулов нюанс.",
                Price = 10.50m,
                Weight = "0.9 кг",
                Image = "./images/rhodope.jpg",
                InStock = true,
                Rating = 4.9,
                Tag = "Релакс & Сън"
            },
            new()
            {
                BeekeeperId = bDobrudja.Id,
                Name = "Златен Слънчогледов Пчелен Прашец",
                Type = "Пчелен прашец",
                Category = "pollen_dried",
                Origin = "Добруджа",
                Description = "Ярко жълт прашец, събран от слънчогледовите масиви на Добруджа. Изключително богат на рутин и биофлавоноиди за здраво сърце.",
                HarvestDate = "Реколта Август 2026",
                Purity = "98% Монофлорен слънчоглед",
                TastingNotes = "Сладникав с леки орехови нотки.",
                Price = 7.90m,
                Weight = "250 г",
                Image = "./images/pollen.jpg",
                InStock = true,
                Rating = 4.85,
                Tag = "За Сърце & Тонус"
            },

            // --- ТРАКИЯ ---
            new()
            {
                BeekeeperId = bTrakiya.Id,
                Name = "Тракийски Акациев Мед",
                Type = "Мед",
                Category = "acacia",
                Origin = "Тракия (Садово / Пловдив)",
                Description = "Кристално чист и прозрачен мед от бели акациеви гори по поречието на Марица. Идеален за деца, спортисти и хора с чувствителен стомах.",
                HarvestDate = "Реколта Май 2026",
                Purity = "93.5% Бяла акация",
                TastingNotes = "Копринено нежен, ефирен сладък вкус без киселинност.",
                Price = 10.74m,
                Weight = "0.9 кг",
                Image = "./images/acacia.jpg",
                InStock = true,
                Rating = 4.9,
                Tag = "Най-Търсен"
            },
            new()
            {
                BeekeeperId = bTrakiya.Id,
                Name = "Тракийски Пролетен Пчелен Прашец",
                Type = "Пчелен прашец",
                Category = "pollen_dried",
                Origin = "Тракийска низина",
                Description = "Събран от ранно цъфтящите овощни градини, глухарче и акация в Тракия. Мощен имуностимулатор при сезонна умора.",
                HarvestDate = "Реколта Май 2026",
                Purity = "100% Натурален пролетен сбор",
                TastingNotes = "Естествено сладък с плодов послевкус.",
                Price = 8.20m,
                Weight = "250 г",
                Image = "./images/pollen.jpg",
                InStock = true,
                Rating = 4.9,
                Tag = "Имунитет"
            },

            // --- ДУНАВСКА РАВНИНА ---
            new()
            {
                BeekeeperId = bDanube.Id,
                Name = "Крайдунавски Липов Мед",
                Type = "Мед",
                Category = "linden",
                Origin = "Дунавска равнина (гр. Русе)",
                Description = "Наситен и уханен монофлорен мед от вековните липови гори по поречието на Дунав. Незаменим при простуди, кашлица и грип.",
                HarvestDate = "Реколта Юни 2026",
                Purity = "92.0% Сребролистна липа",
                TastingNotes = "Силно изразен аромат на цъфнала липа със свеж ментов оттенък.",
                Price = 9.80m,
                Weight = "0.9 кг",
                Image = "./images/acacia.jpg",
                InStock = true,
                Rating = 4.88,
                Tag = "Противовирусен"
            },

            // --- СТАРА ПЛАНИНА ---
            new()
            {
                BeekeeperId = bBalkan.Id,
                Name = "Балкански Горски Букет Мед",
                Type = "Мед",
                Category = "herbal",
                Origin = "Стара планина (Тревненски балкан)",
                Description = "Многоцветен полифлорен мед от девствените гори на Трявна – дива детелина, малина, къпина и жълт кантарион.",
                HarvestDate = "Реколта Юли 2026",
                Purity = "95% Диворастящи балкански билки",
                TastingNotes = "Балансиран, плътен горски вкус с билкови нюанси.",
                Price = 10.10m,
                Weight = "0.9 кг",
                Image = "./images/rhodope.jpg",
                InStock = true,
                Rating = 4.95,
                Tag = "Чиста Природа"
            }
        };

        context.Products.AddRange(products);
        context.SaveChanges();
    }
}
