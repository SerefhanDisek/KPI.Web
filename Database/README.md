# Paylaşılan başlangıç verileri

Güncel `09_shared_data_20261122.json` paketi, `08_shared_data_20261112.json` kaynaklarının değerlendirme test verileri çıkarılmış sürümüdür. `employee_evaluation`, `employee_evaluation_detail`, `evaluation_employee_response` tabloları ve bu kayıtlara ait denetim satırları aktarılmaz. Yeni kurulumlarda değerlendirme listeleri boş başlar; mevcut veritabanındaki değerlendirmeler değiştirilmez veya silinmez. Çalışanlar, meslekler, dönemler, KPI soruları, hedefler, izinler, hesaplar ve amir atamaları aynı kaynak satırlarını kullanır.

Önceki paket repository içinde değiştirilmeyen tarihsel kaynak olarak tutulur ve build/publish çıktısına dahil edilmez. Mevcut veritabanı ile `kpi_evaluation_db.bak` dosyasındaki ikinci/tam yedek kümesinden hazırlanmıştır. Kaynak yedek ayrı bir veritabanına açılıp güncel EF şemasına yükseltilmiştir; asıl yedek değiştirilmemiştir. Kaynak sırası `current`, ardından `backup` şeklindedir.

Paketin `Version` değeri Infrastructure `SharedDataSeeder.Version` sabitiyle aynı olmalıdır. `Sources` dizisindeki her kaynak ad ve tablo-satır sözlüğü içerir. Kimlikler yalnızca kaynak içindeki ilişkileri tarif eder; hedef kimliklere yeniden eşlenir. Kullanıcı parolaları ve parola özetleri dışarı alınmaz. SQL yedeği, migration geçmişi ve seed geçmişi pakete dahil edilmez.

## Eşleştirme ve koruma

- Roller kod, hesaplar kullanıcı adı, meslekler ve dönemler ad, şirket hedefleri ad ve dönem ile eşleşir. Adlar Türkçe harf kurallarıyla karşılaştırılır.
- Çalışanlar tekil ad soyadla eşleşir; birden fazla aday varsa meslek de tekil eşleşmelidir. Aynı adlı farklı mesleğe taşınmış tek bir mevcut çalışan çoğaltılmaz.
- KPI kimliği başlık, Herkes seçimi ve eski/yeni ekip bağlantılarının tam kümesidir. Aynı kaynakta bulunan farklı tarihsel sorular korunur; birden fazla hedef aday varsa özgün oluşturulma zamanı tekil eşleşmelidir.
- Değerlendirmeler, soru puanları ve çalışan yanıtları başlangıç verisi değildir; yalnızca uygulama kullanımı veya açık Excel aktarımıyla oluşturulur. Seed mevcut değerlendirmeleri değiştirmez veya silmez.
- Mevcut ekip atamaları, açıkça boşaltılan atamalar dahil, korunur. Yalnızca yeni ekiplerin kaynak atamaları taşınır. Mevcut izin bakiyesi önceliklidir.
- Önce mevcut hedef, sonra paketteki kaynak sırası önceliklidir. Var olan kaydın açıklaması, pasifliği, parolası veya yetkisi paketle ezilmez. Yalnızca sıfırdan kurulumda bootstrap tarafından oluşturulan hesap/rollerin parola dışı bilgileri paketle tamamlanır.
- Ana kayıtlara ait denetim kayıtları özgün içerikleriyle eklenir; geçmiş payload ve metin kimlikleri yeniden yazılmaz. Değerlendirme test verilerine ait denetim kayıtları yeni paketten çıkarılmıştır; veritabanındaki mevcut denetim geçmişi korunur. Aktarımın kendi değişiklikleri de normal atomik denetime tabidir.

Aktarım ve sürüm işareti tek serializable transaction içinde tamamlanır. Hata veya belirsizlikte kısmi veri kalmaz. Aynı sürüm tekrar çalışmaz; sonradan yapılan değişiklikler ve kalıcı silmeler korunur. Önceki `shared-data-20261112-v1` sürümü uygulanmışsa `shared-data-20261122-v2` yalnızca uygulanmış olarak işaretlenir; bu sürüm yeni ana kayıt getirmediğinden aktarım tekrarlanmaz ve mevcut değerlendirmeler korunur. Eski oluşturma SQL dosyaları uyumluluk için tutulur, paket tamamlandığında uygulanmış olarak işaretlenir; metin normalizasyonu tekrar çalışabilir.

## Gelecekte yeni başlangıç verisi dağıtmak

Kaynak paketteki tarihsel soru kimliklerini değiştirmeden, `KpiDuplicateCleanup` başlangıç düzeltmesi paket aktarımından sonra ve uygulama hazır sayılmadan önce çalışır. `kpi-duplicate-cleanup-20261112-v1` işaretiyle bir kez uygulanır. Başlık yazımı, boşluklar ve son nokta/soru işareti farkları tek başına yeni soru sayılmaz. Yalnızca aynı kapsam ve tanım alanları eşleşen aktif tekrarlar pasife alınır. Bir kopyada hedef/kategori boşsa mevcut dolu bilgilerin tamamını içeren kayıt korunur; çelişen dolu değerler birleştirilmez. Uygun kayıtlar arasında en çok kullanılan, ardından en eski soru açık bırakılır. Diğer ekipler, farklı tanımlar, geçmiş soru kimlikleri, puanlar ve anlık görüntüler korunur. Böylece hem yeni kurulumda hem güncellemede gerçek tekrarlar aktif listelerde görünmez. API ve Excel aynı kimlik kuralıyla yeni tekrarları önler.

Uygulanmış paketi yerinde değiştirerek tekrar çalıştırmayın veya hedefin seed geçmişini silmeyin. Gerekli yeni veriler için yeni dosya/sürüm ve açık eşleştirme kuralları hazırlayın; build/publish içeriğini güncelleyin. Hem boş veritabanında hem yerel ekleme/değişiklik içeren veritabanında veri koruma, tekrar başlatma, silinmiş kayıt ve atomik geri alma kontrollerini çalıştırın.

Bu paket kullanıcıların gelecekte girdiği verileri taşımaz. Canlı veritabanları için SQL Server yedekleme ve geri yükleme süreci ayrıca sürdürülmelidir. Güncel paket personel bilgisi içerir; kaynak/publish erişimi buna göre sınırlandırılmalıdır.
