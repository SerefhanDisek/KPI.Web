# KPI Web

KPI Web; Domain, Application, Infrastructure ve ASP.NET Core Web katmanlarını tek repository içinde barındıran bağımsız bir KPI ve performans değerlendirme uygulamasıdır. Eski masaüstü uygulamasına veya repository dışındaki herhangi bir projeye bağımlı değildir.

## Gereksinimler

- .NET 10 SDK
- Aynı bilgisayarda kurulu ve hizmeti çalışan SQL Server / SQL Server Express veritabanı motoru (yalnızca SSMS kurulumu yeterli değildir)
- Projeyi çalıştıran Windows hesabının SQL Server'a giriş, ilk kurulumda veritabanı oluşturma ve şema güncelleme izinleri

## Çalıştırma

Repository kökünde:

```powershell
dotnet restore KPI.Web.sln
dotnet run --project KPI.Web
```

Tarayıcı adresi geliştirme profilinde `https://localhost:7238` olarak açılır.

## Veritabanı bağlantısı

Varsayılan olarak `ConnectionStrings:KpiDatabase` boştur. Uygulama bilgisayardaki varsayılan SQL Server, `SQLEXPRESS` ve kayıtlı diğer isimli instance'ları otomatik bulur ve Windows hesabıyla bağlanır. Erişilebilen mevcut `kpi_evaluation_db` veritabanına öncelik verir; yoksa veritabanı oluşturma izni bulunan bir instance seçer. Yerel bağlantı shared memory üzerinden yapılır; SQL Server Browser, TCP/IP, port veya güvenlik duvarı ayarı gerektirmez.

Proje açıldığında veritabanı, tablolar, başlangıç verileri ve ilk kullanıcılar otomatik hazırlanır. Elle SQL betiği çalıştırmak, SSMS açmak veya `dotnet ef database update` komutu kullanmak gerekmez. Aynı hazırlık publish çıktısında da çalışır. Sonraki açılışlarda mevcut veritabanı güncellenir; kayıtlar ve kullanıcı parolaları korunur.

SQL Server izinleri kurulum sırasında verilmiş olmalıdır; uygulama kendisine sunucu yetkisi vermez. SQL Server kullanılamıyorsa arayüz açık kalır, API açıklayıcı bir 503 yanıtı verir ve hazırlık işlemi 15 saniye arayla yeniden denenir. Sorun giderildikten sonra projeyi yeniden başlatmadan sayfadaki **Tekrar dene** işlemi kullanılabilir.

Uzak sunucu, SQL kullanıcı adı/parolası veya belirli bir instance kullanmak için `ConnectionStrings:KpiDatabase` değerini environment variable ile tanımlayabilirsiniz. Açıkça verilen bağlantı başka bir sunucuya otomatik yönlendirilmez:

```powershell
$env:ConnectionStrings__KpiDatabase = "Server=.\SQLEXPRESS;Database=kpi_evaluation_db;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True"
dotnet run --project KPI.Web
```

İlk çalıştırmada yönetici, 1. amir, 2. amir ve İnsan Kaynakları hesapları oluşturulur. Geliştirme ortamında geçici parolalar oluşturma sırasında uygulama konsolunda gösterilir. `Authentication__BootstrapPassword` ile açıkça yapılandırılmadıkça sabit bir başlangıç parolası yoktur.

Geliştirme ortamında yapılandırılmış başlangıç parolası, `admin` giriş formunda otomatik doldurulur. Üretimde parola otomatik doldurulmaz.

| Hesap | Kullanıcı adı | Başlangıç yetkisi |
|---|---|---|
| Sistem Yöneticisi | `admin` | Tüm yetkiler |
| Kader Güveren Dişek | `kader.guveren.disek` | Gösterge paneli ve değerlendirme geçmişini görüntüleme |

Girişten sonra uygulama doğrudan açılır; zorunlu parola değiştirme adımı yoktur. Kullanıcı istediğinde sağ üstteki kullanıcı menüsünden **Parola değiştir** ekranını açabilir. Eski kayıtlardaki parola değiştirme bayrağı giriş veya ekran erişimini engellemez. Başlangıç işlemi mevcut kullanıcıların parolalarını değiştirmez. Üretimde yeni hesapların oluşturulması için `Authentication__BootstrapPassword` environment variable'ı ile güçlü bir başlangıç parolası verilmesi zorunludur. Üretim loglarına başlangıç parolası yazılmaz. Üretim oturum çerezi HTTPS gerektirir.

Yönetici, Yetkilendirme ekranından yeni rol oluşturabilir, rolü aktif/pasif yapabilir, role yetki dağıtabilir ve kullanıcı hesaplarına rol atayabilir. Sistem rol kodları uygulamadaki veri kapsamı davranışlarını korumak için değiştirilemez.

Yetkilendirme ekranı **Kullanıcı hesapları**, **Rol ve yetki yönetimi** ve **Ekip ve amir bağlılıkları** bölümlerini içerir. Kullanıcı düzenlerken çalışan bağlantısı veya parola alanı gösterilmez; mevcut bağlantı ve parola korunur. Yeni hesap oluştururken ilk giriş parolası belirlenir. Kullanıcılar parolalarını kullanıcı menüsündeki **Parola değiştir** işlemiyle değiştirir. Yetkilendirme yönetimi yetkisiyle her aktif ekibe (meslek grubuna) 1. ve 2. amir atanabilir. Kaydetme, pasif kayıtlar dahil ekibin tüm çalışanlarına uygulanır. Sonradan eklenen veya ekibe taşınan çalışanlar da ekip amirlerini devralır. 1. amir, çalışanın doğrudan bağlı olduğu amirdir; değerlendirmeyi önce kaydetmek bu sırayı değiştirmez. Amir rollerinden birindeki veya Sistem Yöneticisi rolündeki aktif hesaplar her iki konuma da atanabilir; iki amir farklı olmalıdır. Amir sırası ekipteki atamaya göre belirlenir; hesabın rol adı ağırlığı değiştirmez. Geçişte ekipteki mevcut atamalar aynıysa ekip bağlılığına taşınır. Farklı atamalar korunur ve ekip listesinde açıklanır; ekip amirleri açıkça seçildiğinde tüm çalışanlar eşitlenir. Yeniden başlatma, temizlenmiş atamaları geri getirmez. Amir kartlarında aktif bağlı çalışanların ekipleri gösterilir.

Aktif **Sistem Yöneticisi** tüm uygulama yetkilerine her zaman sahiptir; bu rolün yetkileri kaldırılamaz. Diğer roller Yetkilendirme ekranında tanımlanan yetkileri kullanır.

## Değerlendirme ve izin takibi

- Yönetim listelerinde yalnızca aktif kayıtlar gösterilir. Pasife alınan satır listeden çıkar; sonraki satırlar boşluk bırakmadan yukarı gelir. Kayıtlar ve geçmiş değerlendirmeler korunur.
- **İşlem** alanı bulunan listelerde kalem simgesi ilgili kaydın işlem menüsünü açar. Çalışanlar, meslekler, dönemler, şirket hedefleri, değerlendirmeler ve yetkilendirme aynı menü görünümünü kullanır. Menü seçenekleri mevcut yetkilere göre gösterilir; amir değerlendirme penceresinde yalnızca ayrıntı görüntüleme sunulur. Çalışan, meslek, dönem, şirket hedefi, kullanıcı ve rol menülerinde Düzenle, Pasife al ve Sil işlemleri bulunur; rol menüsünde ayrıca Yetkileri yönet yer alır. Bağlantılı kayıtların silinmesi engellenir. Mevcut yetkilerle sistem rolleri ve kendi hesabı için de işlem yapılabilir; oturumun hesabı veya rolü pasife alındığında oturum kapanır.
- KPI soruları **Herkes** seçeneğiyle tüm çalışanlara veya birden fazla **ekibe (meslek grubuna)** atanabilir. Mevcut tek meslek seçimleri korunur.
- **Yeni KPI ölçütü** formunda başlık, açıklama, ekip kapsamı, sıra ve aktiflik bilgileri bulunur. Yeni sorular standart 1–5 puanlamayla değerlendirilir; sonuç yüzde olarak gösterilir. Mevcut KPI kayıtları düzenlenirken formda gösterilmeyen eski ölçüm ve ek bilgiler korunur.
- **KPI Yönetimi** listesinde kalem simgesine tıklandığında açılan **Düzenle**, **Pasife al** ve **Sil** işlemleri KPI yönetimi yetkisiyle kullanılabilir. Pasife alma ve kalıcı silme onay ister; arama ve ekip filtreleri korunur. Değerlendirmelerde kullanılan KPI silinemez, pasife alınabilir. Kullanılmamış KPI kalıcı silinir; başlangıç verisinden gelen silinmiş sorular uygulama yeniden açıldığında geri gelmez.
- Soru başlıklarında her kelimenin ilk harfi Türkçe kurallarına göre büyük gösterilir ve yeni sorular bu biçimde kaydedilir. KPI listesi, değerlendirme ve Excel çıktıları aynı yazımı kullanır; mevcut kısaltmalar ve tarihsel kayıtların saklanan metni korunur.
- **Geçici Kaydet**, eksik puanları da saklar ve kaydı aynı ekranda açık tutar. **Değerlendirme Yap** ekranında aynı çalışan ve dönem seçildiğinde kayıtlı puanlar, gerçekleşen değerler ve notlar yüklenir; yeniden kaydetmek aynı değerlendirmeyi günceller. Değerlendirmeler ekranındaki düzenleme işlemi de kullanılabilir.
- Her amir aynı çalışan/dönem için kendi değerlendirmesini ayrı kaydeder. **Değerlendirme Yap** ekranı oturumdaki kişinin kendi taslağına devam eder; diğer amirin kaydı bunu engellemez. İlk kayıttaki KPI başlıkları, hedefleri ve ağırlıkları korunur. Başka amirin kaydı düzenlenemez; sistem yöneticisi geçmiş listesinden diğer amirin değiştirilebilir kaydını açıkça seçebilir. Aynı kişinin onaylı çalışan/dönem kaydı için yeni boş form açılmaz. Eski, değerlendiren kullanıcısı bulunmayan kayıtlar korunur.
- İki sekmede aynı taslak açılmışsa eski sürümdeki değişiklikler güncel kaydı ezmez. Uyarı sonrasında yerel puan ve notlar ekranda kalır; **Taslağı yeniden yükle** ile güncel kayıt açık onayla alınabilir. API istemcileri, düzenleme yanıtındaki `updatedAt` değerini değiştirmeden güncelleme isteğinin `expectedUpdatedAt` alanında göndermelidir; ilk güncellemede değer `null` olabilir.
- KPI kısa notları çok satırlıdır ve yazdıkça büyür; kayıtlı uzun notlar da açılışta alan boyutuna yansır. Notlar en fazla 2.000 karakterdir ve ayrıntıda satır sonları korunur.
- **Değerlendirme Yap** ekranında seçilen manuel puana tekrar tıklamak seçimi kaldırır; puan özeti güncellenir, notlar korunur. Eksik puan varken kalıcı kayıt yapılamaz.
- **Değerlendirmeler** ekranındaki çalışan, meslek ve dönem filtreleri açılır kutulardan birden fazla seçim yapılmasını destekler. Filtreler birlikte uygulanır; ad değişikliklerinden etkilenmemek için kayıt kimlikleri kullanılır.
- Gösterge panelindeki amir kartlarında **Değerlendirmeleri görüntüle** işlemi, seçili dönemde ilgili amirin hazırladığı ve kullanıcının erişebildiği kayıtları dialog içinde açar. Kartlar, amir rolüne ek olarak çalışan-amir atamalarını ve kayıtlı değerlendirmelerin yazarlarını da dikkate alır; bir amirin Sistem Yöneticisi rolüne geçirilmesi kartını veya geçmiş puanlamalarını kaldırmaz. Ayrı Çalışan ve Ekip açılır filtrelerinde birden fazla seçim yapılabilir; seçimler durum filtresiyle birlikte otomatik uygulanır; **Ayrıntıyı görüntüle** ile KPI puanları ve notlar incelenir. Listeye dönüşte filtreler korunur. Bu görünüm düzenleme veya onaylama yetkisi vermez.
- **Kalıcı Kaydet**, oluşturma ve onaylama yetkisiyle kaydı doğrudan onaylar; ayrıca onaya gönderme adımı yoktur. Kalıcı kaydın puanları değiştirilemez. Değerlendirmeler listesindeki Sil işlemiyle onaylı kaydı yalnızca onaylayan kişi, mevcut onaylama ve geçmiş yetkileriyle kalıcı silebilir. Onaysız kaydı silmek için oluşturma/geçmiş yetkileri ve mevcut düzenleme kapsamı gerekir. Değerlendirmelerde pasife alma yoktur; silme, puanları ve çalışan yanıtlarını da kaldırır, iç denetim geçmişi korunur.
- **İzin Takibi**, yalnızca **Excel yükle**, **Şablon indir** ve çalışan izinlerini listeleme işlemlerini sunar. Çalışan, ekip ve tarih filtreleri ile sayfalama kullanılabilir; elle ekleme, düzenleme ve pasife alma ekranları yoktur. İzin bakiyesi veya iş günü hesabı yapılmaz.
- Yetkilendirme ekranında **İzin takibi · Görüntüle** ve **İzin listesi · Excel yükleme** izinleri rollere atanır. Excel yüklemek için ikisi de gereklidir. Yalnızca görüntüleme yetkisi yükleme yetkisi vermez.
- **Çalışanlar**, **KPI Yönetimi** ve **Değerlendirmeler** sayfalarında, başlığın altındaki **Excel işlemleri** alanında **Excel dışa aktar**, **Şablon indir** ve **Excel içe aktar** düğmeleri bulunur. Kayıt yönetme yetkisi olmayan kullanıcılar içe aktarma ve şablon düğmelerini açıklamasıyla birlikte pasif görür. Dışa aktarma seçili filtrelerdeki tüm kayıtları içerir.
- Şablonların **Kullanım** sayfasında alan biçimleri, kodlar ve kayıt kuralları; yardımcı sayfalarda çalışan, ekip, dönem ve KPI referansları bulunur. Yalnızca ilk veri sayfası okunur. `.xlsx`, 10 MB ve 10.000 veri satırı sınırı uygulanır; formül yerine değer kullanılmalıdır.
- Çalışan, KPI ve izin aktarımında boş **ID** yeni kayıt oluşturur; dolu **ID** mevcut kaydı günceller. Aynı ID dosyada tekrar edemez. **Aktif=Hayır** kayıtları pasif yapar; yükleme öncesi değişiklik onayı istenir. Başarılı kayıtlar saklanır, hatalı kayıtlar satır numarasıyla raporlanır. Yeniden yüklerken başarıyla oluşturulmuş satırları dosyadan çıkarın.
- Değerlendirmelerde her KPI ayrı satırdır; aynı değerlendirmeye ait satırlar aynı **Aktarım Grubu** değerini ve üst bilgileri kullanır. Aynı çalışan/dönem için oturumdaki amire ait kayıt varsa atlanır. İçe aktarma varsayılan olarak geçici kayıt oluşturur; **Kalıcı kaydet** seçeneği oluşturma ve onaylama yetkileriyle çalışır. **Durum**, **Değerlendiren** ve **Değerlendiren Kullanıcı ID** sütunları bilgilendirme amaçlıdır; içe aktarma başka bir amir adına kayıt oluşturmaz. Amir erişim kapsamı ve güncel KPI/puan doğrulamaları korunur.
- Yeni değerlendiren sütunlarını içermeyen eski değerlendirme şablonları da okunabilir; kayıt yine oturumdaki amire ait oluşturulur.

## Onaylı sonuçların karşılaştırılması

- **Onaylı Amir Sonuçları**, erişilebilen onaylı amir sonuçlarını yan yana gösterir. Fark, en yüksek ve en düşük başarı yüzdesi arasındaki yüzde puandır. Aynı çalışan ve dönemin iki onaylı değerlendirmesi tamamlandığında **birleşik başarı = 1. amir × %70 + 2. amir × %30** hesaplanır. Sonuç iki ondalık basamağa yuvarlanır. Gösterge panelindeki birleşik sıralama ve ortalama da aynı hesabı kullanır. Eksik değerlendirmelerde sonuç bekleniyor olarak gösterilir; aynı konumda birden fazla onaylı kayıt varsa açıklama gösterilir. Ayrı amir puanları korunur. Değerlendirme anındaki amir sırası saklandığı için sonraki atamalar geçmiş ağırlıkları değiştirmez. Amir sırası belirlenemeyen eski kayıtlar ayrı sonuç olarak görünür.
- Değerlendirme ayrıntısındaki **Çalışan görüşü** alanında, hesabına çalışan kaydı bağlı olan kişi kendi onaylı değerlendirmesine görüş ve kabul ekleyebilir. Geçmiş korunur ve puanlar değişmez. İtiraz süreci yoktur.
- Uygulama dönemsel değerlendirmeye odaklanır. **Bireysel Hedefler ve Gelişim** ekranı, görüşmeler ve `/api/development` işlemleri kaldırılmıştır; mevcut veriler ve tarihsel migration'lar korunur. Eski izin numarası 18 yeniden kullanılmaz ve izin ekranından gizlenir.
- Ekranların ayrı izinleri **Yetkilendirme** sayfasından atanır. Sistem yöneticisi bunların tümüne sahiptir; mevcut diğer roller otomatik olarak yeni yetki kazanmaz.
- **Değerlendirme Görevleri**, **Yetkinlik / Kıdem Matrisi** ve **Denetim Kayıtları** ekranları ve HTTP işlemleri kaldırılmıştır. Görev bildirimleri de kaldırılmıştır. Önceki matris ve denetim verileri korunur; uygulamanın iç denetim kayıt mekanizması devam eder. Eski izin numaraları 13, 15 ve 17 yeniden kullanılmaz ve izin ekranından gizlenir.
- Sayfa tasarımcısı, özel sayfa menüleri ve `/api/pages` işlemleri kaldırılmıştır. Önceki sayfa verileri ve tarihsel migration'lar silinmemiştir. Eski izin numarası 9 yeniden kullanılmaz ve izin ekranından gizlenir.

## API güvenliği

Çerez kullanan tüm API yazma işlemleri için `/api/auth/csrf` üzerinden alınan token `X-CSRF-TOKEN` başlığında gönderilir. Ortak arayüz yardımcısı bunu otomatik yapar; JSON, giriş/çıkış ve Excel dosya yükleme işlemleri aynı korumayı kullanır. API yanıtları önbelleğe alınmaz. İçerik güvenliği, çerçeveleme ve içerik türü başlıkları Web katmanında merkezi olarak uygulanır.

## Mimari

Öncelikli ürün geliştirme sırası ve tamamlanma durumu [ROADMAP.md](ROADMAP.md) dosyasında takip edilir.

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

Bu yapı BiUM paketi, BiApp servisleri veya repository dışındaki projelere referans gerektirmez. Şema değişiklikleri Infrastructure migration'larıyla uygulanır; mevcut kayıtlar korunur.

Repository yapısı:

```text
KpiEvaluation.Domain          Domain model ve iş kavramları
KpiEvaluation.Application     CQRS, MediatR, DTO ve repository sözleşmeleri
KpiEvaluation.Infrastructure  Repository, EF Core, migration ve teknik uygulamalar
KPI.Web                       ASP.NET Core API ve web arayüzü
Database                      Başlangıç SQL dosyaları
```
