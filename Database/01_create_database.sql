-- SQL Server Management Studio üzerinde master veritabanına bağlıyken çalıştırılabilir.
IF DB_ID(N'kpi_evaluation_db') IS NULL
BEGIN
    CREATE DATABASE [kpi_evaluation_db];
END;
