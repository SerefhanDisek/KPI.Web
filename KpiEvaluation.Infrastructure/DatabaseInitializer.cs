using Microsoft.EntityFrameworkCore;

namespace KpiEvaluation.Infrastructure;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseInitializer(KpiDbContext db) : IDatabaseInitializer
{
    private const string InitialMigrationId = "20260906151758_InitialCreate";
    private static readonly string[] SeedScripts =
    [
        "03_seed_professions.sql",
        "04_seed_employees.sql",
        "05_normalize_kpi_text.sql",
        "05_seed_kpi_criteria.sql",
        "06_seed_evaluation_period.sql",
        "07_seed_strategic_kpi_data.sql"
    ];

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await PrepareLegacyDatabaseAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);

        var databaseDirectory = Path.Combine(AppContext.BaseDirectory, "Database");
        foreach (var scriptName in SeedScripts)
        {
            var scriptPath = Path.Combine(databaseDirectory, scriptName);
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException($"Başlangıç verisi dosyası bulunamadı: {scriptName}", scriptPath);

            var sql = await File.ReadAllTextAsync(scriptPath, cancellationToken);
            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
    }

    private async Task PrepareLegacyDatabaseAsync(CancellationToken cancellationToken)
    {
        if (!await db.Database.CanConnectAsync(cancellationToken)) return;

        var hasApplicationTables = await db.Database
            .SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'profession', N'U') IS NULL THEN 0 ELSE 1 END AS [Value]")
            .SingleAsync(cancellationToken) == 1;
        if (!hasApplicationTables) return;

        var hasMigrationHistory = await db.Database
            .SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'__EFMigrationsHistory', N'U') IS NULL THEN 0 ELSE 1 END AS [Value]")
            .SingleAsync(cancellationToken) == 1;
        if (hasMigrationHistory) return;

        var addSnapshotColumnsSql = """
            IF COL_LENGTH(N'employee_evaluation', N'EmployeeNameSnapshot') IS NULL
                ALTER TABLE employee_evaluation ADD EmployeeNameSnapshot nvarchar(200) NULL;
            IF COL_LENGTH(N'employee_evaluation', N'ProfessionNameSnapshot') IS NULL
                ALTER TABLE employee_evaluation ADD ProfessionNameSnapshot nvarchar(100) NULL;
            IF COL_LENGTH(N'employee_evaluation', N'PeriodNameSnapshot') IS NULL
                ALTER TABLE employee_evaluation ADD PeriodNameSnapshot nvarchar(100) NULL;

            IF COL_LENGTH(N'employee_evaluation_detail', N'KpiTitleSnapshot') IS NULL
                ALTER TABLE employee_evaluation_detail ADD KpiTitleSnapshot nvarchar(1000) NULL;
            IF COL_LENGTH(N'employee_evaluation_detail', N'IsCommonSnapshot') IS NULL
                ALTER TABLE employee_evaluation_detail ADD IsCommonSnapshot bit NULL;
            IF COL_LENGTH(N'employee_evaluation_detail', N'DisplayOrderSnapshot') IS NULL
                ALTER TABLE employee_evaluation_detail ADD DisplayOrderSnapshot int NULL;
            """;

        var legacyUpgradeSql = $$"""

            UPDATE evaluation
            SET EmployeeNameSnapshot = employee.FullName,
                ProfessionNameSnapshot = profession.Name,
                PeriodNameSnapshot = period.Name
            FROM employee_evaluation evaluation
            INNER JOIN employee ON employee.Id = evaluation.EmployeeId
            INNER JOIN profession ON profession.Id = employee.ProfessionId
            INNER JOIN evaluation_period period ON period.Id = evaluation.EvaluationPeriodId
            WHERE evaluation.EmployeeNameSnapshot IS NULL
               OR evaluation.ProfessionNameSnapshot IS NULL
               OR evaluation.PeriodNameSnapshot IS NULL;

            ALTER TABLE employee_evaluation ALTER COLUMN EmployeeNameSnapshot nvarchar(200) NOT NULL;
            ALTER TABLE employee_evaluation ALTER COLUMN ProfessionNameSnapshot nvarchar(100) NOT NULL;
            ALTER TABLE employee_evaluation ALTER COLUMN PeriodNameSnapshot nvarchar(100) NOT NULL;

            UPDATE detail
            SET KpiTitleSnapshot = criterion.Title,
                IsCommonSnapshot = criterion.IsCommon,
                DisplayOrderSnapshot = criterion.DisplayOrder
            FROM employee_evaluation_detail detail
            INNER JOIN kpi_criterion criterion ON criterion.Id = detail.KpiCriterionId
            WHERE detail.KpiTitleSnapshot IS NULL
               OR detail.IsCommonSnapshot IS NULL
               OR detail.DisplayOrderSnapshot IS NULL;

            ALTER TABLE employee_evaluation_detail ALTER COLUMN KpiTitleSnapshot nvarchar(1000) NOT NULL;
            ALTER TABLE employee_evaluation_detail ALTER COLUMN IsCommonSnapshot bit NOT NULL;
            ALTER TABLE employee_evaluation_detail ALTER COLUMN DisplayOrderSnapshot int NOT NULL;

            IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'ck_kpi_order')
                ALTER TABLE kpi_criterion ADD CONSTRAINT ck_kpi_order CHECK ([DisplayOrder] > 0);
            IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'ck_total_score')
                ALTER TABLE employee_evaluation ADD CONSTRAINT ck_total_score CHECK ([TotalScore] > 0);

            CREATE TABLE [__EFMigrationsHistory] (
                [MigrationId] nvarchar(150) NOT NULL,
                [ProductVersion] nvarchar(32) NOT NULL,
                CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
            );
            INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
            VALUES (N'{{InitialMigrationId}}', N'10.0.0');
            """;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // SQL Server bir batch'i çalıştırmadan önce derlediği için yeni eklenen sütunlar
            // aynı batch içinde kullanılamaz. DDL ve veri taşıma adımları ayrı tutulur.
            await db.Database.ExecuteSqlRawAsync(addSnapshotColumnsSql, cancellationToken);
            await db.Database.ExecuteSqlRawAsync(legacyUpgradeSql, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
