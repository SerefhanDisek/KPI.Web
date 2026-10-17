IF NOT EXISTS (SELECT 1 FROM evaluation_period WHERE "Name" = N'2026 - 2. Dönem')
BEGIN
    INSERT INTO evaluation_period("Name","StartDate","EndDate")
    VALUES (N'2026 - 2. Dönem',N'2026-07-01',N'2026-12-31');
END;
