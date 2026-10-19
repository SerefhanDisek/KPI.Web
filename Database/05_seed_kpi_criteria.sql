WITH source AS (
SELECT * FROM (VALUES
(N'Yazılım Mühendisi',N'Günlük Task Girişi: Mevcut yıl içinde girişi yapılmış',1),
(N'Yazılım Mühendisi',N'Personel Çalışma Süreleri: VPN bilgilerine göre personelin bağlantı süreleri, ik bilgilerine göre izin bilgileri,KolayİK dan giriş çıkış ekranı aktif kullanma süreleri, fazla mesai süreleri',2),
(N'Yazılım Mühendisi',N'Çözülen Performans Bug Sayısı',3),(N'Yazılım Mühendisi',N'Ekibe Açılan Technical Bug Sayısı',4),
(N'Yazılım Mühendisi',N'Çözülen KKT bug sayısı: Kara Kutu Testi (KKT) (Black Box Testing) yöntemiyle tespit edildikten sonra yazılım geliştiriciler tarafından başarıyla düzeltilen (fixlenen) toplam yazılım hatası (bug) sayısını ifade eder.',5),
(N'Yazılım Mühendisi',N'Şirketimizin ilgili dönemde stratejileri arasında olan ve uygulamaya alınması hususunda karar verilen projelerin/taleplerin planlanan sürede tamamlanması',6),
(N'Yazılım Mühendisi',N'Uygulama Güvenliği farkındalık eğitiminin yazılımcılar tarafından asgari tamamlanma oranı',7),(N'Yazılım Mühendisi',N'Projelerde ölçülen asgari kalite güvence temsilcisi skoru',8),(N'Yazılım Mühendisi',N'İç müşteri memnuniyeti anketi sonuçlarının optimum seviyenin üzerinde olması',9),(N'Yazılım Mühendisi',N'Statik kod analizi ve kod güvenliği alanlarında çözülmemiş durumda kritik ve blocker bulgusu olmaması',10),(N'Yazılım Mühendisi',N'Yaygınlaştırma sonrası Uygulama Geliştirme Ekipleri kaynaklı hata meydana gelmemesi',11),
(N'Test Mühendisi',N'Manuel Girilen Test Senaryo Sayısı',1),(N'Test Mühendisi',N'QA Tarafından Yazılan Otomasyon Senaryo Sayısı',2),(N'Test Mühendisi',N'Hata Sızma Oranı (Defect Leakage): Canlı ortama (üretime) kaçan hata sayısının, test aşamasında bulunan toplam hata sayısı',3),(N'Test Mühendisi',N'Test Koşum Yüzdesi (Test Execution Rate): Planlanan toplam manuel test senaryosundan, sprint içinde fiilen koşulanların oranı',4),(N'Test Mühendisi',N'İlk Koşum Başarı Oranı (First-Run Pass Rate): İlk kez koşulan test senaryolarından doğrudan "Başarılı (Passed)" olarak geçenlerin yüzdesi',5),(N'Test Mühendisi',N'Test Döngü Süresi (Test Cycle Time): Testlerin başlama anı ile son "Go/No-Go" (Canlıya Çıkış) kararı arasında geçen toplam süre',6),
(N'Destek Uzmanı',N'lk Yanıt Süresi (FRT - First Response Time): Kullanıcının destek talebi açtıktan sonra ekibin ilk geri dönüşü yapana kadar geçen ortalama süre.',1),(N'Destek Uzmanı',N'İlk Temasta Çözüm Oranı (FCR - First Contact Resolution): Taleplerin ek bir yazışmaya veya üst ekibe aktarılmaya gerek kalmadan, ilk yanıtta çözülme yüzdesi',2),(N'Destek Uzmanı',N'Ortalama Çözüm Süresi (MTTR - Mean Time to Resolution): Bir destek biletinin (ticket) açılışından tamamen kapatılmasına kadar geçen ortalama süre',3),(N'Destek Uzmanı',N'Yeniden Açılan Bilet Oranı (Ticket Reopen Rate): "Çözüldü" olarak kapatılan ancak sorunun devam etmesi nedeniyle kullanıcı tarafından tekrar açılan ticketların yüzdesi',4),(N'Destek Uzmanı',N'Birinci seviye destek ekibinin çözemeyip yazılım geliştirme (Ar-Ge) veya ürün ekibine aktarmak zorunda kaldığı ticketların oranı',5),(N'Destek Uzmanı',N'Kullanıcı Başına Düşen Ticket Sayısı: Belirli bir dönemde yazılımı kullanan kişi başına açılan hata veya destek talebi yoğunluğu',6),
(N'İş Analisti',N'Yazılımcı veya test ekibinin eksik analiz nedeniyle Sprint içinde analiste geri döndüğü (Re-open veya Blocked olan) story yüzdesi',1),(N'İş Analisti',N'İş biriminden gelen, regülasyona uymayan veya sistem mimarisini bozacak verimsiz talepleri (veri ve mantık çerçevesinde) reddebilme veya alternatif çözüm sunarak kaynak tasarrufu sağlama sayısı.',2),(N'İş Analisti',N'Sorumlu olunan modülde (örn: Faiz hesaplama, Swaption değerleme, limit aşımı), analiz eksikliği nedeniyle canlıda finansal kayba, hatalı muhasebeleşmeye veya yanlış rapora yol açan hata sayısı',3),(N'İş Analisti',N'BDDK, SPK veya TCMB mevzuat değişikliklerinin, sorumlu olunan modüle etkisini iş birimi talep etmeden proaktif olarak raporlama ve analizini başlatma süresi.',4),(N'İş Analisti',N'Time to Market (Pazara Sunma Süresi): Bir ürün fikrinin analizinden canlıya çıkışına kadar geçen toplam süre.',5),(N'İş Analisti',N'Product Backlog Health (Backlog Sağlığı): "Hazır" (Ready) statüsündeki, analizi tam yapılmış işlerin sonraki sprintler için yeterlilik oranı.',6),
(N'DevOps Mühendisi',N'Sistem Ayakta Kalma Süresi): Sunucuların ve platformun erişilebilir olduğu zaman yüzdesi (Örn: %99.99).',1),(N'DevOps Mühendisi',N'MTTR (Mean Time to Repair / Ortalama Onarım Süresi): Sistemde meydana gelen bir çökme veya arızanın ne kadar sürede çözüldüğü.',2),(N'DevOps Mühendisi',N'Deployment Frequency (Canlıya Çıkış Sıklığı): Günlük, haftalık veya aylık olarak canlı ortama yapılan başarılı dağıtım sayısı.',3),(N'DevOps Mühendisi',N'Infrastructure Cost Efficiency (Altyapı Maliyet Verimliliği): Sunucu ve bulut (Cloud) maliyetlerinin bütçe ve kullanıcı ölçeğine göre optimize edilmesi.',4)
) AS values_source(profession_name,title,display_order)
), expanded AS (
 SELECT p."Id" profession_id,s.title,s.display_order FROM source s JOIN profession p ON p."Name"=s.profession_name
 UNION ALL SELECT p."Id",s.title,s.display_order FROM source s JOIN profession p ON p."Name"=N'Product Owner' WHERE s.profession_name=N'İş Analisti'
)
INSERT INTO kpi_criterion("ProfessionId","Title","IsCommon","DisplayOrder")
SELECT e.profession_id,e.title,CAST(0 AS bit),e.display_order
FROM expanded e
WHERE NOT EXISTS (
    SELECT 1 FROM kpi_criterion k
    WHERE k."ProfessionId"=e.profession_id AND k."Title" COLLATE Turkish_CI_AS=e.title COLLATE Turkish_CI_AS
);

WITH common AS (
SELECT * FROM (VALUES
(N'Diğer ekiplerle yürütülen ortak projelerde, iletişim kazalarından kaynaklanan gecikme veya kriz sayısı.',1),
(N'Teknik ekipler için geliştirilen çözümlerin ortak alanlarda belgelenmesi; iş teslim ederken yapılan bilgilendirmelerin eksiksizliği',2),
(N'Dönem sonlarında iş arkadaşları ve yöneticilerden alınan "Yetişebilirlik, yardımseverlik ve takım çalışmasına yatkınlık" anket puanı',3)
) AS common_source(title,display_order)
)
INSERT INTO kpi_criterion("ProfessionId","Title","IsCommon","DisplayOrder")
SELECT NULL,c.title,CAST(1 AS bit),c.display_order FROM common c
WHERE NOT EXISTS (SELECT 1 FROM kpi_criterion k WHERE k."ProfessionId" IS NULL AND k."Title" COLLATE Turkish_CI_AS=c.title COLLATE Turkish_CI_AS);
