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
