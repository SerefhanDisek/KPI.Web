INSERT INTO profession ("Name")
SELECT v.name
FROM (VALUES
    (N'Yazılım Mühendisi'),
    (N'Test Mühendisi'),
    (N'Destek Uzmanı'),
    (N'İş Analisti'),
    (N'Product Owner'),
    (N'DevOps Mühendisi')
) AS v(name)
WHERE NOT EXISTS (SELECT 1 FROM profession p WHERE p."Name" = v.name);
