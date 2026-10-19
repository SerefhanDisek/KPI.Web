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

## Değerlendirme ve izin takibi

- KPI soruları **Herkes** seçeneğiyle tüm çalışanlara veya birden fazla **ekibe (meslek grubuna)** atanabilir. Mevcut tek meslek seçimleri korunur.
- **Geçici Kaydet**, eksik puanları da saklar. Değerlendirmeler ekranındaki düzenleme işlemiyle devam edilir.
- **Kalıcı Kaydet**, oluşturma ve onaylama yetkisiyle kaydı doğrudan onaylar; ayrıca onaya gönderme adımı yoktur. Kalıcı kayıt değiştirilemez.
- Yetkilendirme ekranında **İzin takibi · Görüntüle** ve **İzin takibi · Kayıt yönetimi** izinleri rollere atanır. Yalnızca görüntüleme yetkisi olan kişiler kayıt değiştiremez. Yönetim için iki izin de gereklidir.
- İzin takibinde çalışan, tarih aralığı, izin türü ve açıklama tutulur; kayıtlar silinmeden pasife alınabilir. İzin bakiyesi veya iş günü hesabı yapılmaz.
- İzin Excel'i yükleme ve indirme, dosya formatı netleştirildikten sonra eklenecektir.

## Mimari

```text
Tarayıcı
  → ASP.NET Core Controller
  → MediatR Command / Query
  → Command Handler / Query Handler
  → Application repository sözleşmesi
  → Infrastructure Repository / EF Core
  → SQL Server
```

Web katmanı iş kuralı veya doğrudan EF Core sorgusu içermez. Ayrıntılı proje kuralları için `RULES.md` dosyasına bakın.

Her işlem `Features/<Özellik>/Commands/<İşlem>` veya `Features/<Özellik>/Queries/<İşlem>` altında kendi istek ve handler dosyasında tanımlanır. Handler'lar `Application/Repositories` sözleşmelerini kullanır; EF Core sorguları, iş doğrulamaları ve transaction yönetimi `Infrastructure/Repositories` içinde uygulanır. Controller'lar yalnızca HTTP girdisini alır, MediatR üzerinden isteği gönderir ve sonucu döndürür.

Bu yapı BiUM paketi, BiApp servisleri veya repository dışındaki projelere referans gerektirmez. Mevcut API adresleri, DTO'lar, yetki kontrolleri ve veritabanı şeması korunur.

Repository yapısı:

```text
KpiEvaluation.Domain          Domain model ve iş kavramları
KpiEvaluation.Application     CQRS, MediatR, DTO ve repository sözleşmeleri
KpiEvaluation.Infrastructure  Repository, EF Core, migration ve teknik uygulamalar
KPI.Web                       ASP.NET Core API ve web arayüzü
Database                      Başlangıç SQL dosyaları
```
