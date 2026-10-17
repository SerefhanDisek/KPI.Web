# KPI Web

KPI Web; Domain, Application, Infrastructure ve ASP.NET Core Web katmanlarını tek repository içinde barındıran bağımsız bir KPI ve performans değerlendirme uygulamasıdır. Eski masaüstü uygulamasına veya repository dışındaki herhangi bir projeye bağımlı değildir.

## Gereksinimler

- .NET 10 SDK
- Erişilebilir SQL Server / SQL Server Express

## Çalıştırma

Repository kökünde:

```powershell
dotnet restore KPI.Web.sln
dotnet run --project KPI.Web
```

Tarayıcı adresi geliştirme profilinde `https://localhost:7238` olarak açılır.

## Veritabanı bağlantısı

Varsayılan bağlantı `KPI.Web/appsettings.json` içindeki `ConnectionStrings:KpiDatabase` değeridir. Ortama özel bağlantıyı dosyaya parola yazmadan environment variable ile değiştirebilirsiniz:

```powershell
$env:ConnectionStrings__KpiDatabase = "Server=.\SQLEXPRESS;Database=kpi_evaluation_db;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True"
dotnet run --project KPI.Web
```

Uygulama açılışta mevcut migration ve başlangıç verilerini uygular. SQL Server geçici olarak erişilemezse web arayüzü açık kalır ve bağlantı durumu ekranda gösterilir.

İlk çalıştırmada yönetici, 1. amir, 2. amir ve İnsan Kaynakları hesapları otomatik oluşturulur. Geliştirme ortamındaki başlangıç hesapları aşağıdadır:

| Hesap | Kullanıcı adı | Geçici parola | Başlangıç yetkisi |
|---|---|---|---|
| Sistem Yöneticisi | `admin` | `KpiAdmin!2026` | Tüm yetkiler |
| Kader Güveren Dişek | `kader.guveren.disek` | `KpiAdmin!2026` | Gösterge paneli ve değerlendirme geçmişini görüntüleme |

Geçici parola ilk girişte değiştirilmek zorundadır. Dağıtım ortamında `Authentication__BootstrapPassword` environment variable'ı ile farklı ve güçlü bir başlangıç parolası verilmelidir.

Yönetici, Yetkilendirme ekranından yeni rol oluşturabilir, rolü aktif/pasif yapabilir, role izin dağıtabilir ve kullanıcı hesaplarına rol atayabilir. Sistem rol kodları uygulamadaki veri kapsamı davranışlarını korumak için değiştirilemez.

## Mimari

```text
Tarayıcı
  → ASP.NET Core Controller
  → MediatR Command / Query
  → Application servis sözleşmesi
  → Infrastructure / EF Core
  → SQL Server
```

Web katmanı iş kuralı veya doğrudan EF Core sorgusu içermez. Ayrıntılı proje kuralları için `RULES.md` dosyasına bakın.

Repository yapısı:

```text
KpiEvaluation.Domain          Domain model ve iş kavramları
KpiEvaluation.Application     CQRS, MediatR, DTO ve servis sözleşmeleri
KpiEvaluation.Infrastructure  EF Core, migration ve teknik uygulamalar
KPI.Web                       ASP.NET Core API ve web arayüzü
Database                      Başlangıç SQL dosyaları
```
