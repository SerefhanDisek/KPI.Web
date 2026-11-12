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

Yetkilendirme ekranı **Kullanıcı hesapları**, **Rol ve yetki yönetimi** ve **Ekip ve amir bağlılıkları** bölümlerini içerir. Kullanıcı düzenlerken çalışan bağlantısı veya parola alanı gösterilmez; mevcut bağlantı ve parola korunur. Yeni hesap oluştururken ilk giriş parolası belirlenir. Kullanıcılar parolalarını kullanıcı menüsündeki **Parola değiştir** işlemiyle değiştirir. Yetkilendirme yönetimi yetkisiyle her aktif ekibe (meslek grubuna) istenen sayıda amir atanabilir ve her amirin etki oranı belirlenebilir. Oranlar pozitif, en fazla iki ondalık basamaklı ve toplam %100 olmalıdır. Amir ekleme ve kaldırma aynı pencereden yapılır. Kaydetme, pasif kayıtlar dahil ekibin tüm çalışanlarına uygulanır. Sonradan eklenen veya ekibe taşınan çalışanlar da ekip amirlerini devralır. Bir amirin oranı değiştirildiğinde kalan yüzde diğer amirlerin mevcut oranlarına göre orantılı paylaştırılır; iki amirde %70 → %69 değişikliğinde %30 → %31 olur. Üç amirde %50/%30/%20 dağılımının ilk oranı %60 yapılırsa diğerleri %24/%16 olur. Yuvarlama sonrası toplam tam %100 kalır. Satırdaki küçük **X** amiri kaldırır; kalan oranlar orantılı olarak %100’e tamamlanır, tek amir kalırsa oranı %100 olur. Değişiklikler **Kaydet** ile uygulanır, vazgeçmek mevcut atamaları değiştirmez. Amir sırası ekipteki listeden belirlenir; değerlendirmeyi önce kaydetmek sırayı veya oranları değiştirmez. Amir rollerinden birindeki veya Sistem Yöneticisi rolündeki aktif hesaplar her amir sırasına atanabilir; aynı kişi bir ekipte birden fazla kez seçilemez. Her atanmış amir ekibin çalışanlarını değerlendirebilir; hesabın rol adı ağırlığı değiştirmez. İlk iki atama eski API alanlarıyla eşzamanlı tutulur. Geçişte ekipteki mevcut atamalar aynıysa ekip bağlılığına taşınır. Farklı atamalar korunur ve ekip listesinde açıklanır; ekip amirleri açıkça seçildiğinde tüm çalışanlar eşitlenir. Yeniden başlatma, temizlenmiş atamaları geri getirmez. Amir kartlarında aktif bağlı çalışanların ekipleri gösterilir.

Aktif **Sistem Yöneticisi** tüm uygulama yetkilerine her zaman sahiptir; bu rolün yetkileri kaldırılamaz. Diğer roller Yetkilendirme ekranında tanımlanan yetkileri kullanır.

## Değerlendirme ve izin takibi

- Yönetim listelerinde yalnızca aktif kayıtlar gösterilir. Pasife alınan satır listeden çıkar; sonraki satırlar boşluk bırakmadan yukarı gelir. Kayıtlar ve geçmiş değerlendirmeler korunur.
- **İşlem** alanı bulunan listelerde kalem simgesi ilgili kaydın işlem menüsünü açar. Çalışanlar, meslekler, dönemler, şirket hedefleri, değerlendirmeler ve yetkilendirme aynı menü görünümünü kullanır. Menü seçenekleri mevcut yetkilere göre gösterilir; amir değerlendirme penceresinde ayrıntı görüntüleme ve yalnızca değerlendiren kişiye görünen Sil işlemi sunulur. Çalışan, meslek, dönem, şirket hedefi, kullanıcı ve rol menülerinde Düzenle, Pasife al ve Sil işlemleri bulunur; rol menüsünde ayrıca Yetkileri yönet yer alır. Bağlantılı kayıtların silinmesi engellenir. Mevcut yetkilerle sistem rolleri ve kendi hesabı için de işlem yapılabilir; oturumun hesabı veya rolü pasife alındığında oturum kapanır.
- KPI soruları **Herkes** seçeneğiyle tüm çalışanlara veya birden fazla **ekibe (meslek grubuna)** atanabilir. Mevcut tek meslek seçimleri korunur.
- **Yeni KPI ölçütü** ve **KPI ölçütünü düzenle** formlarında başlık, açıklama, isteğe bağlı **Bağlı olduğu şirket hedefi**, ekip kapsamı, sıra ve aktiflik bilgileri bulunur. Bağlantı KPI üzerinden yönetilir: aynı hedefe birden fazla KPI bağlanabilir; KPI hedefe bağlanmadan da kaydedilebilir. Hedef seçimi ortak aramalı açılır alanı kullanır, dönem bilgisiyle ayırt edilir ve temizlendiğinde bağlantı kaldırılır. Düzenlemede mevcut hedef seçili gelir; bağlı hedefin adı KPI listesinde gösterilir. Başlık ve açıklama çok satırlıdır; yazı alanın sonuna geldiğinde satır kayar ve alan büyür, metin silindiğinde küçülür. Kayıtlı uzun metinler de açılışta boyutlandırılır. Başlık 1.000, açıklama 2.000 karakterle sınırlıdır. KPI listesinde Ölçüm ve Ağırlık sütunları gösterilmez. Yeni sorular standart 1–5 puanlamayla değerlendirilir; sonuç yüzde olarak gösterilir. Mevcut KPI kayıtları düzenlenirken formda gösterilmeyen eski ölçüm ve ek bilgiler korunur.
- **KPI Yönetimi** listesinde kalem simgesine tıklandığında açılan **Düzenle**, **Pasife al** ve **Sil** işlemleri KPI yönetimi yetkisiyle kullanılabilir. Pasife alma ve kalıcı silme onay ister; arama ve ekip filtreleri korunur. Değerlendirmelerde kullanılan KPI silinemez, pasife alınabilir. Kullanılmamış KPI kalıcı silinir; başlangıç verisinden gelen silinmiş sorular uygulama yeniden açıldığında geri gelmez.
- Soru başlıklarında her kelimenin ilk harfi Türkçe kurallarına göre büyük gösterilir ve yeni sorular bu biçimde kaydedilir. KPI listesi, değerlendirme ve Excel çıktıları aynı yazımı kullanır; mevcut kısaltmalar ve tarihsel kayıtların saklanan metni korunur.
- **Geçici Kaydet**, eksik puanları da saklar ve kaydı aynı ekranda açık tutar. **Değerlendirme Yap** ekranında aynı çalışan ve dönem seçildiğinde kayıtlı puanlar, gerçekleşen değerler ve notlar yüklenir; yeniden kaydetmek aynı değerlendirmeyi günceller. Değerlendirmeler ekranındaki düzenleme işlemi de kullanılabilir.
- Her amir aynı çalışan/dönem için kendi değerlendirmesini ayrı kaydeder. Toplam puan soru sayısından bağımsız olarak 100 üzerinden gösterilir; örneğin 33/45, 73,33/100 olur. Standart soruların azami katkısı 100 / soru sayısıdır; eksik sorular sıfır katkıyla hesaba katılır. **Değerlendirme Yap** ekranı oturumdaki kişinin kendi taslağına devam eder; diğer amirin kaydı bunu engellemez. İlk kayıttaki KPI başlıkları, hedefleri ve ağırlıkları korunur. Başka amirin kaydı düzenlenemez; sistem yöneticisi geçmiş listesinden diğer amirin değiştirilebilir kaydını açıkça seçebilir. Aynı kişinin onaylı çalışan/dönem kaydı için yeni boş form açılmaz. Eski, değerlendiren kullanıcısı bulunmayan kayıtlar korunur.
- İki sekmede aynı taslak açılmışsa eski sürümdeki değişiklikler güncel kaydı ezmez. Uyarı sonrasında yerel puan ve notlar ekranda kalır; **Taslağı yeniden yükle** ile güncel kayıt açık onayla alınabilir. API istemcileri, düzenleme yanıtındaki `updatedAt` değerini değiştirmeden güncelleme isteğinin `expectedUpdatedAt` alanında göndermelidir; ilk güncellemede değer `null` olabilir.
- KPI kısa notları çok satırlıdır ve yazdıkça büyür; kayıtlı uzun notlar da açılışta alan boyutuna yansır. Notlar en fazla 2.000 karakterdir ve ayrıntıda satır sonları korunur.
- **Değerlendirme Yap** ve **Değerlendirmeyi düzenle** ekranlarında manuel puanlar aynı 1–5 rakam düğmeleriyle seçilir. Kayıtlı puan seçili gelir; aynı puana tekrar tıklamak seçimi kaldırır, notlar korunur. Düğmeler klavyeyle Enter/Boşluk üzerinden de kullanılabilir. Eksik puan geçici kaydedilebilir; kalıcı kayıt için tüm sorular puanlanmalıdır. Sayısal eski KPI'ların otomatik puanlama akışı korunur.
- **Çalışanlar** ekranında **Çalışanlar** ve **Meslekler**; **KPI Yönetimi** ekranında **KPI ölçütleri**, **Kategoriler** ve **Meslekler**; **Ekip ve amir bağlılıkları** bölümünde **Ekipler** ve **Amirler** ayrı çoklu seçim filtreleridir. Seçimler birlikte ve otomatik uygulanır; düzenleme ve liste yenileme sonrasında korunur. Meslek filtresinde seçilen ekiplerin KPI’larına ek olarak **Herkes** kapsamındaki ortak KPI’lar da görünür; diğer başlık ve kategori seçimleri birlikte uygulanır. Kategorisiz KPI'lar ayrı seçilebilir; amir filtresi üçüncü ve sonraki amirleri de kapsar. Excel çıktıları filtrelenen kayıtları içerir.
- Tüm açılır filtrelerde ve ortak seçim alanlarında, mevcut görünüm korunarak seçenek listesinin içine **Seçeneklerde ara…** alanı eklenmiştir. Örneğin **asd** yazıldığında adında bu metin geçen seçenekler listelenir; kayıtlar ancak seçiminizi yaptığınızda filtrelenir. Türkçe büyük/küçük harf eşleşmesi desteklenir; arama sırasında seçilmiş değerler korunur. Aramayı temizlemek tüm seçenekleri yeniden gösterir.
- **Değerlendirmeler** ekranındaki çalışan, meslek ve dönem filtreleri açılır kutulardan birden fazla seçim yapılmasını destekler. Filtreler birlikte uygulanır; ad değişikliklerinden etkilenmemek için kayıt kimlikleri kullanılır.
- Gösterge panelindeki genel özet yalnızca **Toplam çalışan**, **Değerlendirilen** ve **Bekleyen** kutularından oluşur. Amir kartlarında ve amir değerlendirme penceresinin özetinde **Çalışan**, **Onaylı** ve **Bekleyen** gösterilir; ortalama yüzdesi bulunmaz. Birleşik başarı kişi bazında **Birleşik performans sıralaması** içinde **Ortalama Puan** başlığıyla gösterilir. Amir kartlarındaki **Değerlendirmeleri görüntüle** işlemi, seçili dönemde ilgili amirin hazırladığı ve kullanıcının erişebildiği kayıtları dialog içinde açar. Kartlar, amir rolüne ek olarak çalışan-amir atamalarını ve kayıtlı değerlendirmelerin yazarlarını da dikkate alır; bir amirin Sistem Yöneticisi rolüne geçirilmesi kartını veya geçmiş puanlamalarını kaldırmaz. Ayrı Çalışan ve Ekip açılır filtrelerinde birden fazla seçim yapılabilir; seçimler durum filtresiyle birlikte otomatik uygulanır; **Ayrıntıyı görüntüle** ile KPI puanları ve notlar incelenir. Listeye dönüşte filtreler korunur. İşlem menüsündeki **Sil**, kalıcı kayıtlar dahil yalnızca değerlendirmeyi yapan kişiye görünür. Silme onay ister; liste, özetler ve gösterge paneli güncellenirken dönem ve pencere filtreleri korunur. **Düzenle**, mevcut oluşturma yetkisiyle yalnızca değerlendirme sahibinin düzenlenebilir kayıtlarında görünür ve Değerlendirmeler ekranındaki düzenleyiciyi açar. Kaydetme veya vazgeçme sonrasında seçili dönem ve filtrelerle amirin listesine dönülür; kayıtta panel sayıları da yenilenir. Onaylı kaydı da yalnızca değerlendirmeyi yapan kişi düzenleyebilir; kaydettiğinde kayıt taslağa döner ve yeniden onay gerektiğini belirten bir uyarı penceresi açılır.
- **Kalıcı Kaydet**, oluşturma ve onaylama yetkisiyle kaydı doğrudan onaylar; ayrıca onaya gönderme adımı yoktur. Onaylı kaydın puanlarını yalnızca değerlendirmeyi yapan kişi **Düzenle** ile değiştirebilir; **Taslağa Döndür ve Kaydet**, onay bilgilerini temizler ve yeniden onay gerektiğini bildiren uyarı penceresi gösterir. Oluşturma yetkisi gereklidir; başkasının onaylı kaydını sistem yöneticisi de düzenleyemez. Yeniden onaylanana kadar kayıt onaylı/birleşik sonuçlara katılmaz. Değerlendirmeler listesindeki işlem menüsünde tüm soruları puanlanmış, düzenlenebilir kayıtlar için **Kalıcı Kaydet** bulunur; geçmiş, oluşturma ve onaylama yetkileri gerekir. **Sil** yalnızca değerlendirmeyi yapan kişiye görünür ve sunucu da bu sahipliği denetler. Değerlendiren, kalıcı kaydedilmiş olsa veya başka bir yönetici tarafından onaylansa bile kendi değerlendirmesini geçmiş yetkisiyle silebilir. Sistem yöneticisi dahil başka kişiler silemez. Değerlendirmelerde pasife alma yoktur; silme, puanları ve çalışan yanıtlarını da kaldırır, iç denetim geçmişi korunur.
- **İzin Takibi**, yalnızca **Excel yükle**, **Şablon indir** ve dosyadan alınan izin bakiyelerini listeleme işlemlerini sunar. Excel ve boş şablon **Adı Soyadı, Kalan İzin Gün Sayısı, İzin Hak Ediş Tarihi** sütunlarını içerir. Çalışan adı tek bir aktif kayıtla eşleşirse mevcut kişi kullanılır; sistemde bulunmayan kişi **Meslek atanmadı** mesleğiyle aktif çalışan olarak oluşturulur ve her iki ekranda listelenir. Bu meslek henüz yoksa otomatik oluşturulur. Belirsiz adlar satır hatası verir; aynı adla pasif kayıt varsa yeni çalışan açılmaz ve önce mevcut kaydın etkinleştirilmesi gerekir. Yalnızca personel kaydı oluşturulur, giriş hesabı açılmaz. Yarım günler ve negatif bakiyeler desteklenir; dosyadaki değerler aynen saklanır, hak ediş veya iş günü hesabı yapılmaz. Her çalışan için tek güncel bakiye tutulur; yeniden yükleme kalan günü ve hak ediş tarihini günceller, dosyada bulunmayan çalışanları değiştirmez. Dosyada aynı çalışan adı tekrar edemez. İlk sayfanın adı farklı olabilir, başlıkların üstünde boş satır bulunabilir; Excel tarihleri ile gg.aa.yyyy/yyyy-AA-gg metinleri okunur. Çalışan, ekip ve hak ediş tarihi filtreleri ile sayfalama kullanılabilir. Eski tarih aralıklı izin kayıtları ve ID’li dosya desteği korunur; bu kayıtlar bakiyeye dönüştürülmez.
- Yetkilendirme ekranında **İzin takibi · Görüntüle** ve **İzin listesi · Excel yükleme** izinleri rollere atanır. Excel yüklemek için ikisi de gereklidir. Yalnızca görüntüleme yetkisi yükleme yetkisi vermez.
- **Çalışanlar**, **KPI Yönetimi** ve **Değerlendirmeler** sayfalarında, başlığın altındaki **Excel işlemleri** alanında **Excel dışa aktar**, **Şablon indir** ve **Excel içe aktar** düğmeleri bulunur. Kayıt yönetme yetkisi olmayan kullanıcılar içe aktarma ve şablon düğmelerini açıklamasıyla birlikte pasif görür. Dışa aktarma seçili filtrelerdeki tüm kayıtları içerir.
- Şablonların **Kullanım** sayfasında alan biçimleri, kodlar ve kayıt kuralları; yardımcı sayfalarda çalışan, ekip, dönem ve KPI referansları bulunur. Yalnızca ilk veri sayfası okunur. `.xlsx`, 10 MB ve 10.000 veri satırı sınırı uygulanır; formül yerine değer kullanılmalıdır.
- Çalışan çıktısı ve şablonu yalnızca **Ad Soyad, Meslek, Aktiflik durumu** sütunlarını içerir. Meslek adları **Meslek Referansları** sayfasında ID olmadan listelenir. İçe aktarma Ad Soyad üzerinden aktif veya pasif mevcut çalışanı tekil olarak eşleştirir; eşleşme yoksa yeni personel oluşturur. Meslek belirtilmişse aynı çalışana uygulanır; meslek sistemde yoksa otomatik oluşturulur. Meslek boşsa veya Meslek sütunu yoksa mevcut meslek korunur; yeni çalışan için **Meslek atanmadı** kullanılır. Aynı adla birden fazla çalışan varsa belirtilen meslek tek bir mevcut kaydı eşleştirmelidir; belirsiz eşleşmeler ve dosyada tekrarlanan adlar hata verir. Pasif meslekler otomatik etkinleştirilmez. Yalnızca personel kaydı oluşturulur, giriş hesabı açılmaz. **Aktiflik durumu** Evet/Hayır (1/0, TRUE/FALSE) kabul eder; boş değer Evet sayılır. Ad değişikliği uygulamadaki Düzenle işlemiyle yapılır; meslek değişikliği aynı çalışan kimliğini, izin bağlantılarını ve geçmiş değerlendirme anlık görüntülerini korur. Eski ID/Meslek ID/Aktif içeren çalışan dosyaları da okunur.
- Eski KPI ve tarih aralıklı izin aktarımında boş **ID** yeni kayıt oluşturur; dolu **ID** mevcut kaydı günceller. Aynı ID dosyada tekrar edemez. **Aktif=Hayır** kayıtları pasif yapar; yükleme öncesi değişiklik onayı istenir. Başarılı kayıtlar saklanır, hatalı kayıtlar satır numarasıyla raporlanır. Yeniden yüklerken başarıyla oluşturulmuş satırları dosyadan çıkarın.
- KPI Excel çıktısı ve şablonu yalnızca **KPI, Açıklama, Ekipler, Herkes, Kategori, Bağlı olduğu şirket Hedefi** sütunlarını içerir. Ekip ve hedef referanslarında ID bulunmaz. Birden fazla ekip adı noktalı virgülle ayrılır; **Herkes=Evet** tüm ekipleri kapsar. İçe aktarma başlık ve ekip kapsamı tek bir mevcut KPI ile eşleşirse açıklama, kategori ve hedefi günceller; eşleşme yoksa standart manuel 1–5 puanlı yeni kayıt oluşturur. Mevcut ölçüm, ağırlık, sıra, aktiflik ve diğer gizli ayarlar korunur. Belirsiz eşleşmeler ve dosyada tekrarlanan başlık/kapsam çiftleri satır hatası verir; başlık/kapsam değişiklikleri Düzenle işlemiyle yapılır. Eski ID’li KPI dosyaları da okunur.
- Değerlendirme Excel çıktısı ve şablonu yalnızca **Çalışan, Ekip, Dönem, Not, KPI, Puan, Amir Görüşü, Değerlendiren Amir** sütunlarını içerir. Her KPI ayrı satırdır; puan soru başına 1–5, Not soruya ait açıklama, Amir Görüşü değerlendirmeye ait ortak açıklamadır. Çalışan ve Ekip birlikte, Dönem ve uygun KPI adlarıyla eşleştirilir; yardımcı sayfalarda adlar bulunur ve teknik ID gerekmez. Belirsiz çalışan/KPI adları satır hatası üretir. Aynı değerlendirmenin Amir Görüşü tüm satırlarda aynı olmalıdır. Değerlendiren Amir bilgi/gruplama amaçlıdır; kayıt oturumdaki kullanıcıya ait oluşturulur. Mevcut çalışan/dönem/değerlendiren kayıtları atlanır. İçe aktarma varsayılan olarak taslak oluşturur; kalıcı kayıt normal oluşturma/onaylama yetkileriyle çalışır. Sayısal eski KPI'larda gereken gerçekleşen değer sade dosyada bulunmadığından bu kayıtlar uygulamadan tamamlanır.
- Önceki teknik ID/Aktarım Grubu içeren değerlendirme şablonları, eski isteğe bağlı değerlendiren sütunları olmadan da okunabilir; kayıt yine oturumdaki amire ait oluşturulur. Yeni şablon ve çıktılarda yalnızca sade sekiz sütun bulunur.

## Onaylı sonuçların karşılaştırılması

- **Onaylı Amir Sonuçları**, erişilebilen onaylı amir sonuçlarını yan yana gösterir. Aynı çalışan ve dönem için ekipte tanımlanan tüm amirlerin onaylı değerlendirmeleri tamamlandığında **Ortalama Puan = amir puanları × tanımlı etki oranları toplamı** hesaplanır. Örneğin üç amire %50, %30 ve %20 dağıtılabilir. Sonuç iki ondalık basamağa yuvarlanır. Gösterge panelindeki birleşik sıralama ve ortalama da aynı hesabı kullanır. **Excel işlemleri → Excel dışa aktar** ile seçili dönemlerdeki erişilebilir sonuçlar indirilir. Excel çıktısı ve **Şablon indir** ile alınan boş şablon yalnızca **Çalışan, Ekip, Dönem, Amir Sonuçları, Ortalama Puan** sütunlarını içerir. Amir puanları ve etki oranları aynı hücrede çok satırlı gösterilir; birleşik puan sayısaldır. Ekranda ve Excel'de fark sütunu bulunmaz; teknik ID ve sonuç durumu sütunları ile ayrı amir ayrıntı sayfası kaldırılmıştır. Şablon aynı görüntüleme yetkileriyle indirilir; bu ekranda içe aktarma bulunmaz. Eksik onaylı değerlendirmelerde beklenen amirler açıklanır; eksik oranlar tamamlanmış gibi yeniden ölçeklenmez. Ayrı amir puanları korunur. Yetkilendirme ekranında değiştirilen oranlar mevcut birleşik sonuçlara da uygulanır. Değerlendirmenin ekip anlık görüntüsü ve kayıtlı değerlendiren kimliği kullanılır; kişisel puanlar ve tarihsel amir sırası değiştirilmez. Güncel atamada bulunmayan amirlerin kayıtları ayrı sonuç olarak görünür, birleşik hesaba katılmaz.
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
