# Ürün geliştirme planı

## Geçerli kapsam

- Geçici Kaydet taslağı saklar; Kalıcı Kaydet oluşturma ve onaylama izinleriyle doğrudan onaylar. Yeni bir amir onay zinciri eklenmez.
- İki amir aynı çalışan/dönem için bağımsız değerlendirme yapar; sonuçlar birleştirilmez.
- Çalışan görüşü ve kabulü korunur; itiraz süreci kullanıcı isteğiyle kapsamdan çıkarılmıştır.
- Sayfa tasarımcısı ve buna bağlı low-code geliştirmeleri kullanıcı isteğiyle kaldırılmıştır. Eski sayfa verileri saklanır; menü, tarayıcı akışı ve API erişimi yoktur.
- E-posta ve Teams entegrasyonları kullanıcı isteğiyle kapsam dışıdır.
- Değerlendirme Görevleri, Yetkinlik / Kıdem Matrisi ve Denetim Kayıtları ekranları kaldırılmıştır. İzin ekranı yalnızca Excel yükleme ve listeleme içindir.

## Öncelik ve durum

| Sıra | İş | Durum | Uygulanan kapsam |
|---|---|---|---|
| 1 | Taslak kaydetme ve devam etme | Tamamlandı | Kayıtlı puanlar, notlar, gerçekleşen değerler ve KPI anlık görüntüleri yüklenir. Aynı kayıt güncellenir; eski sekmeler güncel kaydı ezemez. |
| 2 | Değerlendirme durumları | Mevcut onay akışı doğrulandı ve korundu | Taslak eksik puan kabul eder. Kalıcı kayıt tam puanlama ve iki izinle doğrudan onaylanır. Onaylı kayıt değiştirilemez; geçersiz yazma durumları reddedilir. Tarihsel durum değerleri uyumluluk için korunur; yeni paylaşım/onay zinciri eklenmez. |
| 3 | İki amirin ayrı değerlendirmesi | Tamamlandı | Çalışan/dönem/değerlendiren birleşimi benzersizdir. Taslak, onay ve Excel aktarımı amire aittir. Geçmiş kayıtlar korunur, dashboard çalışanları mükerrer saymaz. |
| 4 | Çalışan görüşü ve kabulü | Tamamlandı; itiraz iptal | Yetkili çalışan kendi onaylı değerlendirmesine görüş/kabul ekler. Yanıt geçmişi saklanır; onaylı puanlar ve değerlendirme durumu değişmez. |
| 5 | Denetim kaydı ve güvenlik | Denetim ekranı kullanıcı isteğiyle kaldırıldı | Denetim ekranı ve liste API erişimi yoktur; önceki veriler ve uygulama iç denetimi korunur. CSRF, güvenlik başlıkları, isteğe bağlı parola değişimi ve üretim başlangıç parolası kuralları devam eder. |
| 6 | Döngü, görev ve hatırlatma | Kullanıcı isteğiyle kaldırıldı | Değerlendirme görev ekranı, API erişimi ve görev bildirimleri kaldırıldı. Mevcut dönemler ve amir değerlendirmeleri devam eder. |
| 7 | Kalibrasyon paneli | Karşılaştırma paneli tamamlandı | Erişilebilen onaylı amir sonuçları yan yana gösterilir; yüzde puan farkı hesaplanır. Puan değiştirme ve yeni bir birleşik final skor bu kapsamda yoktur. |
| 8 | Yetkinlik/kıdem matrisi | Kullanıcı isteğiyle kaldırıldı | Menü, tarayıcı ekranı ve API erişimi kaldırıldı; önceki matris kayıtları saklanır. |
| 9 | Bireysel hedefler, check-in, gelişim planları | Tamamlandı | Hedef/plan, tarih, elle girilen ilerleme ve görüşme geçmişi saklanır. Çalışan kapsamı, sürüm kontrolü, atomik ilerleme güncellemesi ve pasife alma uygulanır. |
| 10 | Low-code / sayfa tasarımcısı | Kullanıcı isteğiyle kaldırıldı | Tasarım, yayın, sürümleme, veri kaynağı kataloğu ve grafik bileşeni geliştirmeleri kapsam dışıdır. |
| 11 | Kurumsal entegrasyonlar | E-posta/Teams iptal; SSO/HR uygulanmadı | SSO ve HR için sağlayıcı veya erişim yapılandırması tanımlanmadı. Çalışmayan örnek entegrasyon eklenmedi; mevcut yerel oturum sistemi korunur. |
| 12 | 9-box, succession planning, PIP, kontrollü AI | Sonraki aşama | Henüz uygulanmadı. Potansiyel ölçütü, personel süreçleri ve AI veri/erişim kapsamı tanımlanmadan otomatik personel sınıflandırması yapılmaz. |

## Kullanım ve yetkiler

- Yeni ekranlar ayrı izinlerle yönetilir. Sistem yöneticisi erişebilir; mevcut diğer rollere otomatik yetki verilmez.
- Çalışan görüşü/kabulü için değerlendirme geçmişi ve yanıt izinleri gerekir. Hesabın çalışan kaydıyla ilişkilendirilmesi zorunludur.
- Bireysel gelişimde sistem yöneticisi tüm çalışanları, amir atanmış çalışanlarını, çalışan hesabı kendi kaydını ilgili izinle yönetebilir.
- Gelişim tanımları kullanıcının girdiği verileri saklar; yeni performans veya hak ediş hesabı oluşturmaz.
- Yeni şema geçişleri uygulama açılışındaki mevcut migration akışıyla uygulanır. Eski değerlendirmeler ve eski sayfa verileri silinmez.

## Doğrulama kapsamı

- Eksik taslak, tekrar kaydetme, yeniden açma, eski sekme ve örtüşen veritabanı yazmaları.
- Aynı çalışan/dönem için bağımsız iki amir kaydı; amir bazlı Excel aktarımı ve erişim sınırları.
- Çalışan görüşü/kabulü, itirazın reddedilmesi ve onaylı içeriğin korunması.
- İç denetim aktörü, parola malzemesinin hariç tutulması ve kayıt değişikliğinin engellenmesi.
- Bireysel hedef/plan, görüşmeler, arşivleme, sürüm ve çalışan erişim kontrolleri.
- CSRF ve kimliğe bağlı token kontrolü, güvenlik başlıkları, gerçek tarayıcıda CSP, doğrudan giriş ve isteğe bağlı parola değişimi.
- Kaldırılan sayfa tasarımcısı, görevler, matris ve denetim ekranlarının menü/API üzerinden erişilememesi ve eski verilerin korunması.
- İzin Excel yükleme ve liste yenileme; Türkçe soru baş harfleri, Excel çıktıları ve tarihsel soru metinlerinin korunması.

Son sadeleştirmede 26 API/tarayıcı kontrolü geçti: kaldırılan ekranlar ve uç noktalar, izin Excel yükleme ve liste yenileme, Türkçe soru yazımı, Excel çıktıları ve tarihsel soru metinlerinin korunması. Gerçek SQL Server ve çerez oturumuyla kalan 12 ekran, otomatik dolan geliştirme girişi, isteğe bağlı parola penceresi, çıkış ve yenileme doğrulandı. Kullanıcı parolaları değiştirilmedi. Çözüm derlemesi 0 hata ve 0 uyarıyla tamamlandı. Önceki kapsamda 228 API/tarayıcı ve 29 Excel uyumluluk kontrolü geçmişti; kaldırılan ekranlara ilişkin önceki kontroller artık geçerli kapsamı tanımlamaz.
