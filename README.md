# Пчелари България

Платформа за обединяване на български пчелари: каталог с мед и пчелен прашец, пчелари по региони, количка и поръчки, заявки за регистрация на пчелари, отзиви и администрация.

**Технологии:** ASP.NET Core (.NET 10) Web API · Entity Framework Core · Microsoft SQL Server · HTML/CSS/JavaScript (в `wwwroot`) · xUnit

## Бърз старт

```powershell
cd pcheli-diplomenproekt\pcheli-diplomenproekt

# 1. Данни на администратора (пазят се в User Secrets, не в кода)
dotnet user-secrets set "Admin:Username" "admin"
dotnet user-secrets set "Admin:Password" "<парола>"

# 2. (само първия път) инструмент за миграции
dotnet tool install --global dotnet-ef

# 3. Създаване на базата от миграцията InitialCreate (Data/Migrations)
dotnet ef database update

# 4. Стартиране
dotnet run
```

* Сайт: адресът от конзолата (напр. `http://localhost:5272`)
* Администрация: `/admin.html`
* API документация (Development): `/swagger`
* Connection string: `appsettings.json` → `ConnectionStrings:DefaultConnection` (по подразбиране SQL Server LocalDB, база `PcheliBgDb`; за SQL Server Express напр. `Server=.\SQLEXPRESS;Database=PcheliBgDb;Trusted_Connection=True;TrustServerCertificate=True`)

## Функции

* **Магазин:** търсене, сортиране, филтри по цена/наличност, странициране, любими (гост и акаунт), подаръчни комплекти, произход и партида на продуктите, наличности по опаковка.
* **Акаунти** (`/account.html`): регистрация/вход, история и „Поръчай отново“, адреси, любими, абонаменти за периодична доставка (пауза/прекратяване).
* **Поръчки:** оценка на доставката за Еконт/Спиди (тарифа, не жив API), подарък със съобщение, проследяване по номер + телефон, плащане с карта през Stripe (по желание), имейл известия.
* **Пчелари:** публична страница (`/beekeeper.html?id=`), панел (`/panel.html`) за продукти, наличности, поръчки и снимки. Администраторът свързва акаунт с пчелин.
* **Блог** (`/blog`), `sitemap.xml`, `robots.txt`.
* **Администрация** (`/admin.html`): табло с графики, наличности, поръчки с филтри/странициране, CSV експорт, товарителници за печат, блог, абонаменти, журнал на действията.
* **Акаунт и поверителност:** забравена парола, потвърждение на имейл, изтегляне на личните данни (JSON), изтриване на акаунта (поръчките се запазват анонимно за счетоводство), отказ на поръчка от клиента (само докато е „Приета“ и неплатена), разписка за печат.
* **Правни страници и контакти:** общи условия, поверителност, бисквитки, доставка, връщане (`/terms.html` и др.), информативен банер за бисквитки, форма за контакт и съобщение до пчелин (`/contact.html`).
* **Ангажираност:** бюлетин (с отписване), „Уведоми ме, когато е наличен“, известие по имейл до пчеларя за нова поръчка, автоматично отказване на неплатени картови поръчки след `Payments:PendingHours` (24 ч) с връщане на наличността.
* **SEO:** страници `/p/{id}` и `/b/{id}` с Open Graph и schema.org, разширен `sitemap.xml`; `/health` за мониторинг.
* **Администрация (допълнение):** редакция/скриване/изтриване на пчелари, региони, потребители (блокиране, изпращане на нова парола), съобщения, бюлетин.
* **Сигурност:** rate limiting, вградена CAPTCHA на публичните форми, lockout при грешни пароли.
* **BG/EN и светла/тъмна тема**, PWA (инсталиране, страница „Няма връзка“).

### Допълнителна конфигурация (всичко е по желание)

| Ключ | Предназначение |
|---|---|
| `Stripe:SecretKey`, `Stripe:WebhookSecret` | плащане с карта (без тях опцията е изключена) |
| `Email:Host/Port/User/Password/From/FromName/EnableSsl` | изпращане на писма (без тях се записват в журнала) |
| `Captcha:Enabled`, `Captcha:MinSeconds` | CAPTCHA на формите |
| `Shop:SenderName`, `Shop:SenderPhone` | данни за подател в товарителниците |
| `Shop:CompanyName`, `Shop:CompanyEik`, `Shop:CompanyAddress`, `Shop:ContactEmail`, `Shop:VatNote` | данни на търговеца за правните страници, разписката и контактите – **попълнете преди публикуване** |
| `Payments:PendingHours` | след колко часа неплатена картова поръчка се отказва (по подразбиране 24) |
| `App:PublicUrl` | публичен адрес за връзки в писма и sitemap |
| `RateLimiting:Disabled` | изключва ограниченията (само за тестове) |

Новите миграции се прилагат автоматично при стартиране на приложението.

## Тестове

```powershell
cd pcheli-diplomenproekt
dotnet test
```

## Структура

```
pcheli-diplomenproekt/
├── pcheli-diplomenproekt/        # ASP.NET Core приложението
│   ├── Controllers/  Services/  Dtos/  Models/  Data/ (с Migrations/)
│   └── wwwroot/                  # index.html, app.js, styles.css, admin.html, images/
└── pcheli-diplomenproekt.Tests/  # xUnit тестове
ConsoleApp2/                      # алчен алгоритъм за ресто (раздел 5.8 от дипломната работа)
```
