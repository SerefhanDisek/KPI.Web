# KPI Web — Sunucu kurulum ve işletim notları

Bu belge, uygulamayı sunucuya kuracak teknik ekip içindir. Sunucu işletim sistemi, SQL Server adresi, alan adı ve çalıştırma hesabı henüz bildirilmediğinden aşağıdaki adresler ve yollar örnektir. Gerçek değerleri kurulum ekibi belirlemelidir. Belge mevcut kodun davranışını anlatır; sunucuda kurulum veya ayar değişikliği yapıldığı anlamına gelmez.

## 1. Kurulum ekibinin belirleyeceği bilgiler

| Bilgi | Kurulumda netleştirilecek değer |
|---|---|
| Uygulama sunucusu | Windows sürümü ve IIS kurulumu |
| Yayın adresi | HTTPS alan adı ve geçerli sertifika |
| SQL Server | Sunucu/instance, sürüm, gerekiyorsa TCP portu |
| Veritabanı | Yeni veritabanı mı, mevcut veritabanının taşınması mı? |
| Uygulama hesabı | IIS Application Pool'u çalıştıracak Windows hesabı |
| SQL kimlik doğrulaması | Windows hesabı veya SQL kullanıcı hesabı |
| Kalıcı yayın yolu | Örneğin `C:\Sites\KPI.Web` |
| Başlangıç parolası | Kurulum ekibinin güvenli kanaldan belirleyeceği değer |
| Yedekleme | SQL yedekleme sıklığı, saklama yeri ve sorumlu kişi |

## 2. Teknik gereksinimler

- Proje `net10.0-windows` hedefler. Oturum anahtarlarında Windows DPAPI kullanıldığı için mevcut haliyle Windows üzerinde kurulmalıdır. Linux kurulumu ayrıca geliştirme gerektirir.
- IIS üzerinde çalıştırılacaksa **.NET 10 Hosting Bundle** kurulmalıdır. Yalnızca SDK veya yalnızca SQL Server Management Studio kurulması yeterli değildir. Hosting Bundle, ASP.NET Core Module bileşenini de kurar. IIS sonradan kurulmuşsa Hosting Bundle kurulumunu onarın. [Microsoft IIS rehberi](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0)
- SQL Server motoru çalışır durumda olmalıdır. EF Core sağlayıcısının desteklediği SQL Server 2019 veya sonrası kullanılmalı; özellikle taşınan veritabanlarında compatibility level kontrol edilmelidir. Proje özel bir düşük uyumluluk seviyesi tanımlamıyor. [SQL Server sağlayıcısı](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)
- Kaynak koddan publish alınacak bilgisayarda `global.json` ile uyumlu .NET 10 SDK bulunmalıdır; dosyada `10.0.400`, `latestFeature` seçilidir. Hazır publish paketi çalıştırılacak sunucuda kaynak kod, Visual Studio veya EF komut satırı araçları gerekmez.

## 3. Yayın paketini hazırlama

Kaynak kod teslim edildiyse repository kökünde çalıştırın:

```powershell
dotnet publish .\KPI.Web\KPI.Web.csproj -c Release -o .\artifacts\publish
```

Hazır publish paketi teslim edildiyse yeniden derlemek gerekmez. `publish` klasörünün **tamamını** yayın dizinine taşıyın. Yalnızca `KPI.Web.dll` dosyasını veya yalnızca `wwwroot` klasörünü kopyalamayın.

Paket içinde en az şu dosya ve klasörler bulunmalıdır; bu liste paketin tamamı değildir:

```text
KPI.Web.dll
KPI.Web.deps.json
KPI.Web.runtimeconfig.json
KpiEvaluation.Application.dll
KpiEvaluation.Domain.dll
KpiEvaluation.Infrastructure.dll
web.config
appsettings.json
wwwroot/
Database/
    05_normalize_kpi_text.sql
    09_shared_data_20261122.json
    ... diğer paketlenmiş SQL dosyaları
```

`Database` klasörü `wwwroot` içine konulmamalı ve ayrı bir IIS sanal dizini olarak açılmamalıdır. Başlangıç paketi personel bilgileri içerir. Eski `08_shared_data_20261112.json` tarihsel kaynak olarak repository'de tutulur; güncel publish paketinin parçası değildir.

## 4. Uygulama ayarları

Aşağıdaki environment variable değerlerini **uygulamayı çalıştıran IIS sürecine** tanımlayın. ASP.NET Core, environment variable değerlerini ortak `appsettings.json` ayarlarının üzerine uygular. İç içe ayar adlarında çift alt çizgi kullanılır. [Yapılandırma önceliği](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0#environment-variables)

| Environment variable | Değer / amaç |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `DOTNET_ENVIRONMENT` | Tanımlıysa `Production` ile uyumlu olmalı; `Development` kalmamalı |
| `ConnectionStrings__KpiDatabase` | Hedef SQL Server ve kalıcı uygulama veritabanının bağlantısı |
| `Authentication__BootstrapPassword` | Yeni hesaplar için sunucuya özel güçlü başlangıç parolası |

IIS'te bu değerler siteye ait `system.webServer/aspNetCore/environmentVariables` ayarından veya kurumun uygulama sürecine ortam değişkeni sağlayan dağıtım yöntemiyle verilebilir. Mevcut `web.config` içindeki `aspNetCore` başlatma ve handler ayarlarını koruyun. Ayarlar değişince ilgili Application Pool'u yeniden başlatın. Bir PowerShell penceresinde `$env:...` tanımlamak, halihazırda çalışan IIS sürecini yapılandırmaz.

`launchSettings.json` yerel geliştirme içindir; üretim ayarlarının kaynağı olarak kullanılmamalıdır. Gerçek parolaları ve bağlantı bilgilerini repository'ye veya bu belgeye eklemeyin. Sunucuya özel yapılandırmayı sonraki publish işleminde kaybolmayacak şekilde yönetin.

### Başlangıç parolası hakkında mevcut durum

**Ortak `KPI.Web/appsettings.json` dosyasında şu anda sabit bir geliştirme başlangıç parolası bulunmaktadır.** Bu belge o ayarı değiştirmez. Canlı kurulumda ilk çalıştırmadan önce `Authentication__BootstrapPassword` ile mutlaka farklı ve sunucuya özel bir değer verin. Yalnızca `Production` seçmek dosyadaki sabit parolayı devre dışı bırakmaz.

Parola en az 10 karakter, büyük harf, küçük harf, rakam ve özel karakter içermelidir. Bu değer yeni oluşturulan başlangıç hesaplarında kullanılır. Mevcut kullanıcıların parolalarını değiştirmez; yanlış parolayla bir kez oluşturulan hesaplar yalnızca environment variable değiştirilerek düzeltilmez.

Yeni kurulumdaki kullanıcı adları:

| Kullanıcı adı | Hesap |
|---|---|
| `admin` | Sistem Yöneticisi |
| `deniz.ozyurek` | Deniz Özyürek |
| `melih.yurtsever` | Melih Yurtsever |
| `kader.guveren.disek` | Kader Güveren Dişek |

Üretimde parola otomatik doldurulmaz ve yeni hesap parolaları loglara yazılmaz. Zorunlu ilk giriş parola değiştirme akışı yoktur; başlangıç parolalarını kullanıcılarla güvenli paylaşın ve her hesabın kullanıcı menüsündeki **Parola değiştir** işlemiyle kendi parolasına geçmesini sağlayın.

## 5. SQL Server bağlantısı ve izinler

### Bağlantı örnekleri

Aşağıdaki değerler doğrudan kullanılacak gerçek bağlantılar değildir. `SQL_SUNUCUSU`, `SQL_KULLANICISI` ve `GUVENLI_PAROLA` alanlarını kurulum ekibi doldurmalıdır. Instance veya port bilgisini hedef SQL Server'a göre belirtin.

Windows kimlik doğrulaması:

```text
Server=SQL_SUNUCUSU;Database=kpi_evaluation_db;Integrated Security=True;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True
```

SQL kullanıcı hesabıyla:

```text
Server=SQL_SUNUCUSU;Database=kpi_evaluation_db;User ID=SQL_KULLANICISI;Password=GUVENLI_PAROLA;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True
```

Bu örnekler SQL Server'ın istemci tarafından güvenilen ve sunucu adıyla eşleşen bir sertifika sunmasını gerektirir. Sertifika veya ağ bağlantısı hatalarını DBA ile çözün; geliştirme ortamındaki `Encrypt=False` ayarını üretime otomatik taşımayın.

Windows kimlik doğrulamasında SQL'e bağlanan kişi, kurulumu yapan kullanıcının SSMS hesabı değil **IIS uygulamasını çalıştıran hesaptır**. Ayrı sunucudaki SQL'e erişim için uygun etki alanı/servis hesabı veya SQL kimlik doğrulaması belirlenmelidir. TCP erişimi gerekiyorsa SQL dinleme ayarları ve güvenlik duvarı kuralı kurulum ekibi tarafından hazırlanmalıdır.

Bağlantı ayarı boş bırakılırsa uygulama yalnızca aynı Windows makinesindeki SQL Server instance'larını Windows kimlik doğrulaması ve shared memory ile arar; varsayılan veritabanı `kpi_evaluation_db` olur. Ayrı SQL sunucusunu kendiliğinden bulmaz. Canlıda hedef bağlantıyı açıkça tanımlayın. Açık bağlantı başarısız olduğunda uygulama başka SQL sunucusuna geçmez.

### Gerekli veritabanı izinleri

Mevcut uygulama migration ve başlangıç hazırlığını açılışta kendisi yürütür. Kullanılan SQL kimliği için:

- SQL Server'a giriş ve hedef veritabanına erişim,
- uygulama tablolarında normal okuma/yazma,
- migration'lar için gerekli şema oluşturma/değiştirme ve başlangıç hazırlığı yetkileri gerekir.

Hedef veritabanı henüz yoksa otomatik oluşturma için ayrıca veritabanı oluşturma yetkisi gerekir. DBA boş veritabanını önceden hazırlarsa uygulama hesabına sunucu genelinde veritabanı oluşturma yetkisi vermek gerekmez; hedef veritabanındaki şema hazırlığı yetkileri yine gereklidir. Uygulama kendisine yetki vermez.

Sadece veri okuma/yazma yetkisi verilmesi mevcut açılış akışı için yeterli olmayabilir. `sysadmin` yetkisi zorunlu değildir; gerekli izinleri DBA belirlemelidir. Kurum politikası uygulama hesabının şema değiştirmesine izin vermiyorsa, ayrı dağıtım hesabıyla migration uygulama süreci geliştiriciyle planlanmalıdır; mevcut sürümde bunu kapatan hazır bir ayar yoktur. [Migration dağıtım seçenekleri](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)

## 6. IIS, HTTPS ve dosya izinleri

1. Siteye özel bir Application Pool oluşturun. `.NET CLR Version` için `No Managed Code` seçin ve yayın mimarisiyle uyumlu çalıştırın.
2. IIS fiziksel yolunu publish içeriğinin bulunduğu dizine verin. Uygulamayı `https://kpi.ornek.com/` gibi site kökünden yayınlayın. Tarayıcı kodu `/api` ve statik dosyalar için kök adresler kullanır; `/kpi/` gibi bir alt yoldan yayınlama ayrıca uyarlama gerektirir.
3. HTTPS binding ve geçerli sertifika tanımlayın. HTTP'den HTTPS'e yönlendirmeyi IIS veya önündeki proxy katmanında yapılandırın. Uygulamada otomatik HTTPS yönlendirmesi tanımlı değildir; Production oturum çerezi HTTPS gerektirir.
4. Application Pool hesabına uygulama dosyaları için okuma/çalıştırma; yayın kökündeki `.data-protection` klasörü için okuma/yazma izni verin. Klasör yoksa önceden oluşturup yalnızca gerekli yazma iznini bu klasöre verebilirsiniz.
5. Uygulama Windows DPAPI'yi hesap bazında kullanır. IIS hesabının kullanıcı profili erişimini doğrulayın; Application Pool gelişmiş ayarlarında `Load User Profile=True` yapılandırın. Hesabı ve `.data-protection` klasörünü güncellemelerde koruyun. Başka hesapta veya makinede anahtar dosyalarını yalnızca kopyalamak, eski anahtarların çözülebileceğini garanti etmez. [DPAPI davranışı](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-encryption-at-rest?view=aspnetcore-10.0)

Mevcut kurulum modeli tek Windows uygulama sunucusudur. Birden fazla web sunucusuyla yük dengeleme planlanıyorsa ortak oturum anahtarı yönetimi ayrıca tasarlanmalıdır.

## 7. İlk açılışta gerçekleşen işlemler

Uygulama sırasıyla SQL bağlantısını hazırlar, EF Core migration'larını uygular, metin normalizasyonunu çalıştırır, ilk kullanıcı/rolleri oluşturur, güncel seed paketini aktarır ve sürümlü mükerrer KPI temizliğini tamamlar.

Yeni kurulumda elle SQL dosyası çalıştırmak, eski bir `.bak` dosyasını geri yüklemek veya `dotnet ef database update` komutu çalıştırmak normal kurulum adımı değildir. `Database` dosyalarını değiştirmeden paketle birlikte bulundurmak yeterlidir; gerekli SQL izinleri verilmiş olmalıdır.

- Yeni seed; çalışanları, meslekleri, dönemleri, KPI'ları, şirket hedeflerini, izin bakiyelerini, hesapları ve amir atamalarını içerir.
- **Yeni ve boş veritabanında Değerlendirmeler sayfası boş başlar.** Test değerlendirmeleri, soru puanları ve çalışan yanıtları seed'den çıkarılmıştır.
- Mevcut veritabanına bağlanılır veya eski veritabanı taşınırsa mevcut değerlendirmeler korunur. Yeni seed onları temizlemez.
- Daha önce uygulanmış seed tekrar veri eklemez. Önceki `shared-data-20261112-v1` sürümü bulunan kurulumda yeni `shared-data-20261122-v2` yalnızca uygulanmış olarak işaretlenir.
- `__EFMigrationsHistory` ve `__KpiSeedHistory` tablolarını silmeyin veya boşaltmayın. Bunlar şema/seed işlemlerinin tekrarını kontrol eder.

İlk hazırlık tamamlanana kadar arayüz açılabilir fakat API istekleri HTTP `503` döner. Bu, başarılı kurulum kabul edilmemelidir. Uygulama başarısız hazırlığı yaklaşık 15 saniye aralıkla yeniden dener. Başarı logu: **KPI veritabanı kullanıma hazır.** Ayrıntılı veri davranışı için [Database/README.md](Database/README.md) dosyasına bakın.

## 8. Kurulum sonrası doğrulama

| Kontrol | Beklenen sonuç |
|---|---|
| HTTPS üzerinden ana sayfa | Sertifika uyarısı olmadan giriş ekranı |
| Oturum açmadan `GET /api/auth/me` | Hazırlık bittikten sonra `401`; sürekli `503` olmamalı |
| `GET /api/auth/login-defaults` | Production'da `204`, parola içermeyen boş yanıt |
| Yönetici girişi | Yeni kurulumda `admin` ve sunucuda yapılandırılmış başlangıç parolasıyla başarılı giriş |
| Çalışanlar / meslekler / KPI / izinler | Başlangıç kayıtları ve yetkiye uygun listeler |
| Değerlendirmeler | Yeni boş veritabanında boş liste; taşınmış veritabanında mevcut kayıtlar |
| Oturum işlemleri | Çıkış, tekrar giriş ve sayfa yenileme başarılı |
| Application Pool yeniden başlatma | Kayıtlar korunur, başlangıç kayıtları çoğalmaz |

Yeni seed'in temiz kurulum için toplam başlangıç sayıları 15 meslek, 44 çalışan, 143 KPI, 2 dönem, 5 şirket hedefi, 40 izin bakiyesi ve 4 kullanıcı hesabıdır. Bunlar tarihsel/pasif kayıtları da içerir; aktif listelerde ve mükerrer temizliği sonrasında görünen sayılar daha az olabilir. Mevcut veritabanındaki kullanıcı değişiklikleri nedeniyle bu sayılar farklılaşabilir.

Production kontrolünde gerçek kullanıcı verileriyle deneme değerlendirmesi oluşturmayın. Yazma işlemlerinin ayrıntılı kabul testleri ayrı test veritabanında yapılmalıdır. Kaynak geliştirme ortamında temiz SQL kurulumu, seed tekrar çalıştırma, eski seed'den geçişte veri koruma ve başarısız aktarımın geri alınması doğrulanmıştır; bu, hedef sunucudaki bağlantı ve izin kontrollerinin yerini tutmaz.

## 9. Mevcut veritabanını taşımak

Sıfırdan kurulum ile mevcut veritabanını taşımak farklı işlemlerdir. Güncel yerel kayıtlar sunucuya taşınacaksa DBA, ilgili SQL veritabanının güncel yedeğini alıp hedefe geri yüklemeli ve uygulamayı bu hedefe bağlamalıdır. Kullanıcı/parola bağlantıları ve gerekli SQL erişimleri hedefte yeniden doğrulanmalıdır.

Seed paketi sabit bir başlangıç kümesidir; kaynak bilgisayarda daha sonra girilen kayıtları otomatik taşımaz. Mevcut veritabanının yedeği geri yüklenirse o yedekteki test değerlendirmeleri de gelir ve korunur. Değerlendirmesiz başlangıç isteniyorsa yeni boş veritabanı ve güncel seed kullanılmalıdır; mevcut kayıtlardan veri silme bu teslimin kapsamında değildir.

## 10. Sonraki güncellemeler ve geliştirme

1. Canlı SQL veritabanının yedeğini alın; geri yükleme yönteminin çalıştığını doğrulayın. Mevcut uygulama sürümünü, sunucuya özel yapılandırmayı ve `.data-protection` anahtarlarını koruyun.
2. Geliştiriciden temiz bir dizine alınmış yeni Release publish paketini teslim alın. Şema değişikliği varsa migration içeriği önceden incelenmiş ve canlı veritabanının ayrı test kopyasında denenmiş olmalıdır.
3. Bakım aralığında uygulamaya erişimi durdurun; site/Application Pool durdurularak eski sürümün migration sırasında veritabanına erişmesi önlensin. Yeni paket dosyalarını kopyalayın.
4. Canlı bağlantı bilgisini, Production ayarını ve diğer sunucuya özel değerleri koruyun. `.data-protection` klasörünü silmeyin. Ardından uygulamayı başlatıp hazırlığın tamamlanmasını ve temel kontrolleri doğrulayın.
5. Migration uygulanmış bir veritabanında yalnızca eski DLL'leri geri koymanın güvenli geri dönüş olduğunu varsaymayın. Gerekirse uyumlu uygulama sürümü ve veritabanı yedeği birlikte değerlendirilmelidir; geri yükleme, yedek sonrasındaki kayıtları kaybettirebilir.

Geliştirme/test bağlantıları canlı veritabanından ayrı tutulmalıdır. Yerelde yapılan kod değişiklikleri publish ile, şema değişiklikleri EF migration ile taşınır. Yerel SQL veritabanındaki veri düzenlemeleri kod yayınlamakla canlıya geçmez. Canlı veritabanını sıfırlamak, migration geçmişini silmek veya eski oluşturma SQL dosyalarını elle tekrar çalıştırmak bir güncelleme yöntemi değildir.

Düzenli SQL yedeklerini yayın klasöründen ayrı yerde saklayın. Seed dosyası ve uygulama dosyalarının kopyası, canlı veritabanı yedeğinin yerine geçmez.

## 11. Sık karşılaşılan sorunlar

| Belirti | Önce kontrol edilecek nokta |
|---|---|
| IIS `500.30`, `500.31` veya `502.5` | .NET 10 Hosting Bundle, runtime/mimari uyumu, `web.config` ve başlangıç logları |
| Sayfa açılıyor ama API sürekli `503` | SQL erişimi/izinleri, migration hatası, `Database` dosyalarının eksikliği ve uygulama başlangıç logları |
| SSMS bağlanıyor, uygulama bağlanamıyor | IIS çalıştırma hesabının SQL yetkileri ve uygulama sürecine verilen gerçek bağlantı ayarı |
| SQL sertifika hatası | Güven zinciri, SQL sertifikası ve bağlantıdaki sunucu adı |
| Giriş sonrası tekrar giriş ekranı / çerez sorunu | HTTPS, Production ortamı, `.data-protection` izinleri, DPAPI hesabı ve kullanıcı profili |
| Kurulumdan sonra test değerlendirmeleri görünüyor | Boş veritabanı yerine mevcut/test veritabanına bağlanılması veya yedekten taşınması |
| Veriler kaybolmuş gibi görünüyor | Bağlanılan sunucu/instance ve veritabanı adı; farklı boş veritabanına bağlanılıp bağlanılmadığı |
| `/kpi/` altından sayfa veya API çalışmıyor | Kök adres varsayımı; site kökünden yayınlama veya geliştiriciyle alt yol uyarlaması |

IIS Windows Event Viewer ve uygulama başlangıç loglarını inceleyin. Ayrıntı gerekiyorsa IIS `aspNetCore` stdout loglamasını geçici olarak etkinleştirip log klasörüne gerekli yazma iznini verin; inceleme sonrasında kapatın. Logların ve yapılandırma dosyalarının erişimini sınırlandırın. Hata bildiriminde zaman, HTTP durum kodu ve parola içermeyen hata ayrıntılarını paylaşın.

Kurulum tamamlandığında uygulama URL'si, uygulanan sürüm, SQL hedefi, çalıştırma hesabı ve yedekleme sorumlusu kayıt altına alınmalıdır. Parolalar bu kayıtla birlikte tutulmamalıdır.
