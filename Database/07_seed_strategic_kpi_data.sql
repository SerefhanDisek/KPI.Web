-- Word dokümanındaki stratejik başlıklar. Hedef ve ağırlık değerleri yönetim onayı gerektirdiği için
-- yalnızca açıkça belirtilen ölçüler otomatik doldurulur; kalanları KPI Yönetimi ekranından tamamlanır.
INSERT INTO company_goal (Name,Description,Priority,IsActive)
SELECT v.Name,v.Description,v.Priority,1 FROM (VALUES
 (N'Ürün ve Teknik Kalite',N'Kod, test, güvenlik ve veri kalitesini artırmak',1),
 (N'Zamanında Teslimat',N'Projeleri ve talepleri planlanan zamanda tamamlamak',2),
 (N'Operasyonel Mükemmellik',N'Kullanılabilirlik, otomasyon ve çözüm sürelerini iyileştirmek',3),
 (N'Müşteri Memnuniyeti',N'İç ve dış müşteri deneyimini geliştirmek',4),
 (N'Sürekli Gelişim ve İşbirliği',N'Öğrenme, bilgi paylaşımı ve ekipler arası işbirliğini güçlendirmek',5)
) v(Name,Description,Priority)
WHERE NOT EXISTS (SELECT 1 FROM company_goal g WHERE g.Name=v.Name AND g.EvaluationPeriodId IS NULL);

UPDATE k SET Category=N'Teknik Kalite',CompanyGoalId=g.Id,DataSource=COALESCE(k.DataSource,N'Azure DevOps / SonarQube'),ReviewFrequency=1,
 IsSpecific=1,IsMeasurable=1,IsAchievable=1,IsRelevant=1,IsTimeBound=1
FROM kpi_criterion k CROSS JOIN company_goal g
WHERE g.Name=N'Ürün ve Teknik Kalite' AND (k.Title LIKE N'%kalite%' OR k.Title LIKE N'%bug%' OR k.Title LIKE N'%hata%' OR k.Title LIKE N'%test%');

UPDATE k SET Category=N'Teslimat',CompanyGoalId=g.Id,DataSource=COALESCE(k.DataSource,N'Azure DevOps'),ReviewFrequency=1,
 IsSpecific=1,IsMeasurable=1,IsAchievable=1,IsRelevant=1,IsTimeBound=1
FROM kpi_criterion k CROSS JOIN company_goal g
WHERE g.Name=N'Zamanında Teslimat' AND (k.Title LIKE N'%sprint%' OR k.Title LIKE N'%planlanan sürede%' OR k.Title LIKE N'%Time to Market%');

UPDATE k SET Category=N'Operasyon',CompanyGoalId=g.Id,DataSource=COALESCE(k.DataSource,N'Dynatrace / Operasyon Kayıtları'),ReviewFrequency=0,
 IsSpecific=1,IsMeasurable=1,IsAchievable=1,IsRelevant=1,IsTimeBound=1
FROM kpi_criterion k CROSS JOIN company_goal g
WHERE g.Name=N'Operasyonel Mükemmellik' AND (k.Title LIKE N'%MTTR%' OR k.Title LIKE N'%Ayakta Kalma%' OR k.Title LIKE N'%Çözüm Süresi%' OR k.Title LIKE N'%Yanıt Süresi%');

UPDATE k SET Category=N'Müşteri',CompanyGoalId=g.Id,DataSource=COALESCE(k.DataSource,N'Anket / Destek Sistemi'),ReviewFrequency=1,
 IsSpecific=1,IsMeasurable=1,IsAchievable=1,IsRelevant=1,IsTimeBound=1
FROM kpi_criterion k CROSS JOIN company_goal g
WHERE g.Name=N'Müşteri Memnuniyeti' AND k.Title LIKE N'%müşteri%';

UPDATE k SET Category=N'İşbirliği',CompanyGoalId=g.Id,DataSource=COALESCE(k.DataSource,N'Yönetici Değerlendirmesi'),ReviewFrequency=2,
 IsSpecific=1,IsMeasurable=1,IsAchievable=1,IsRelevant=1,IsTimeBound=1
FROM kpi_criterion k CROSS JOIN company_goal g
WHERE g.Name=N'Sürekli Gelişim ve İşbirliği' AND k.IsCommon=1;

IF NOT EXISTS (SELECT 1 FROM profession WHERE Name=N'Proje Yöneticisi')
 INSERT INTO profession(Name,IsActive) VALUES(N'Proje Yöneticisi',1);

INSERT INTO kpi_criterion(ProfessionId,Title,Description,Category,IsCommon,DisplayOrder,MeasurementType,TargetDirection,WeightPercent,ReviewFrequency,IsActive)
SELECT p.Id,v.Title,v.Description,N'Proje Yönetimi',0,v.SortOrder,0,0,0,1,1
FROM profession p CROSS JOIN (VALUES
 (N'PBI ve Task Story Point bilgilerinin eksiksizliği',N'PBI altındaki tasklarda Story Point bilgisinin eksiksiz tutulması',1),
 (N'Çalışılan talep ve projelerde efor uyumu',N'Tahmin edilen efor ile gerçekleşen eforun karşılaştırılması',2),
 (N'Projelerin planlanan sürede tamamlanması',N'Dönem stratejilerindeki proje ve taleplerin zamanında tamamlanması',3)
) v(Title,Description,SortOrder)
WHERE p.Name=N'Proje Yöneticisi' AND NOT EXISTS(SELECT 1 FROM kpi_criterion k WHERE k.ProfessionId=p.Id AND k.Title COLLATE Turkish_CI_AS=v.Title COLLATE Turkish_CI_AS);
