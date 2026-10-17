using KpiEvaluation.Application;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KpiEvaluation.Infrastructure;

public sealed class DatabaseMaintenanceService(KpiDbContext db, IDatabaseInitializer initializer) : IDatabaseMaintenanceService
{
    public async Task BackupAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ValidationException("Yedek dosyası seçilmelidir.");
        var builder = CreateBuilder();
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName)) throw new ValidationException("Veritabanı adı bulunamadı.");

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 0;
        command.CommandText = $"BACKUP DATABASE [{EscapeIdentifier(databaseName)}] TO DISK = @path WITH INIT, CHECKSUM";
        command.Parameters.AddWithValue("@path", Path.GetFullPath(filePath));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RestoreAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath)) throw new ValidationException("Seçilen yedek dosyası bulunamadı.");
        var builder = CreateBuilder();
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName)) throw new ValidationException("Veritabanı adı bulunamadı.");
        builder.InitialCatalog = "master";

        await db.Database.CloseConnectionAsync();
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        var escapedName = EscapeIdentifier(databaseName);
        try
        {
            await using var restore = connection.CreateCommand();
            restore.CommandTimeout = 0;
            restore.CommandText = $"ALTER DATABASE [{escapedName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                                  $"RESTORE DATABASE [{escapedName}] FROM DISK = @path WITH REPLACE; " +
                                  $"ALTER DATABASE [{escapedName}] SET MULTI_USER;";
            restore.Parameters.AddWithValue("@path", Path.GetFullPath(filePath));
            await restore.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            await using var recover = connection.CreateCommand();
            recover.CommandText = $"IF DB_ID(@databaseName) IS NOT NULL ALTER DATABASE [{escapedName}] SET MULTI_USER";
            recover.Parameters.AddWithValue("@databaseName", databaseName);
            try { await recover.ExecuteNonQueryAsync(cancellationToken); } catch { }
            throw;
        }
        finally
        {
            SqlConnection.ClearAllPools();
        }
        await initializer.InitializeAsync(cancellationToken);
    }

    private SqlConnectionStringBuilder CreateBuilder() =>
        new(db.Database.GetConnectionString()
            ?? throw new ValidationException("Veritabanı bağlantı bilgisi bulunamadı."));

    private static string EscapeIdentifier(string value) => value.Replace("]", "]]", StringComparison.Ordinal);
}
