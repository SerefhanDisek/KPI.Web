using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace KpiEvaluation.Infrastructure;

public sealed class SqlServerConnectionResolver(string? configuredConnectionString, ILogger<SqlServerConnectionResolver> logger)
{
    private const string DatabaseName = "kpi_evaluation_db";
    private string? resolvedConnectionString = string.IsNullOrWhiteSpace(configuredConnectionString) ? null : configuredConnectionString;

    public string ConnectionString => resolvedConnectionString
        ?? throw new InvalidOperationException("Yerel SQL Server bağlantısı henüz hazırlanmadı.");

    public static string GetInitializationFailureMessage(Exception exception) => exception switch
    {
        SqlServerConfigurationException => exception.Message,
        SqlException sql when sql.Number is 18456 or 229 or 262 or 916 =>
            "Windows hesabı veya yapılandırılan SQL kullanıcısı için veritabanına giriş, oluşturma ve şema güncelleme izinlerini kontrol edin.",
        _ => "Veritabanı hazırlanamadı. SQL Server hizmetini, bağlantı ayarlarını ve veritabanı izinlerini kontrol edin. Uygulama otomatik olarak tekrar deneyecek."
    };

    public async Task ResolveAsync(CancellationToken cancellationToken = default)
    {
        // Açıkça yapılandırılmış bağlantı başka bir sunucuya yönlendirilmez.
        if (resolvedConnectionString is not null) return;

        var candidates = await Task.WhenAll(GetLocalServers().Select(server => ProbeAsync(server, cancellationToken)));
        var existing = candidates.Where(x => x is { DatabaseExists: true }).ToArray();
        var selected = existing.FirstOrDefault(x => x!.CanAccessDatabase)
            ?? (existing.Length == 0 ? candidates.FirstOrDefault(x => x is { CanCreateDatabase: true }) : null);

        if (selected is null)
        {
            if (existing.Length > 0 || candidates.Any(x => x is not null))
                throw new SqlServerConfigurationException("SQL Server bulundu ancak Windows hesabının KPI veritabanına erişim veya yeni veritabanı oluşturma izni yok. SQL Server yöneticisi bu hesaba gerekli izinleri vermelidir.");

            throw new SqlServerConfigurationException("Yerel SQL Server'a bağlanılamadı. SQL Server veritabanı motorunun kurulu ve hizmetinin çalışır olduğundan, Windows hesabının SQL Server'a giriş izni bulunduğundan emin olun.");
        }

        resolvedConnectionString = CreateConnectionString(selected.Server).ConnectionString;
        logger.LogInformation("Yerel SQL Server seçildi: {Server}; veritabanı: {Database}. Mevcut veritabanı: {Exists}.",
            selected.Server, DatabaseName, selected.DatabaseExists);
    }

    private IReadOnlyList<string> GetLocalServers()
    {
        var instances = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MSSQLSERVER", "SQLEXPRESS" };
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var key = registry.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
                if (key is not null) instances.UnionWith(key.GetValueNames());
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or System.Security.SecurityException or IOException)
            {
                logger.LogWarning("Yerel SQL Server instance listesi okunamadı ({RegistryView}); standart instance adları denenecek.", view);
            }
        }

        return instances.OrderBy(x => x.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase) ? 0
                : x.Equals("SQLEXPRESS", StringComparison.OrdinalIgnoreCase) ? 1 : 2)
            .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase)
                ? $"lpc:{Environment.MachineName}" : $"lpc:{Environment.MachineName}\\{x}")
            .ToArray();
    }

    private async Task<ServerCandidate?> ProbeAsync(string server, CancellationToken cancellationToken)
    {
        var builder = CreateConnectionString(server);
        builder.InitialCatalog = "master";
        builder.ConnectTimeout = 3;
        builder.ConnectRetryCount = 0;
        builder.Pooling = false;
        try
        {
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 3;
            command.CommandText = """
                SELECT CASE WHEN DB_ID(@database) IS NOT NULL THEN 1 ELSE 0 END,
                       COALESCE(HAS_DBACCESS(@database), 0),
                       CASE WHEN HAS_PERMS_BY_NAME(NULL, 'SERVER', 'CREATE ANY DATABASE') = 1
                                  OR IS_SRVROLEMEMBER('dbcreator') = 1
                                  OR IS_SRVROLEMEMBER('sysadmin') = 1 THEN 1 ELSE 0 END;
                """;
            command.Parameters.AddWithValue("@database", DatabaseName);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            return new ServerCandidate(server, reader.GetInt32(0) == 1, reader.GetInt32(1) == 1, reader.GetInt32(2) == 1);
        }
        catch (SqlException exception)
        {
            logger.LogDebug("Yerel SQL Server bağlantısı kurulamadı: {Server}, SQL hata kodu: {ErrorNumber}.", server, exception.Number);
            return null;
        }
    }

    private static SqlConnectionStringBuilder CreateConnectionString(string server) => new()
    {
        DataSource = server,
        InitialCatalog = DatabaseName,
        IntegratedSecurity = true,
        Encrypt = false,
        TrustServerCertificate = true,
        MultipleActiveResultSets = true
    };

    private sealed record ServerCandidate(string Server, bool DatabaseExists, bool CanAccessDatabase, bool CanCreateDatabase);
}

public sealed class SqlServerConfigurationException(string message) : Exception(message);
