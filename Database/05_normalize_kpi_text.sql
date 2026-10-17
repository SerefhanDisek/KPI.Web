-- Word'den kopyalanan U+F0B7 özel madde işaretini mevcut soru başlıklarından kaldırır.
UPDATE kpi_criterion
SET [Title] = LTRIM(REPLACE([Title], NCHAR(61623), N''))
WHERE CHARINDEX(NCHAR(61623), [Title]) > 0;
