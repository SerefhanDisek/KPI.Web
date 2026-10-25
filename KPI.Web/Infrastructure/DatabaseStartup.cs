using KpiEvaluation.Infrastructure;

namespace KPI.Web.Infrastructure;

public sealed class DatabaseStartup(IServiceProvider services, ILogger<DatabaseStartup> logger, IWebHostEnvironment environment) : BackgroundService
{
    private volatile bool isReady;
    public bool IsReady => isReady;
    public string StatusMessage { get; private set; } = "Veritabanı hazırlanıyor. Lütfen kısa süre sonra tekrar deneyin.";

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await TryInitializeAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!IsReady)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            await TryInitializeAsync(stoppingToken);
        }
    }

    private async Task TryInitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await services.GetRequiredService<SqlServerConnectionResolver>().ResolveAsync(cancellationToken);
            await using var scope = services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync(cancellationToken);
            var accounts = await scope.ServiceProvider.GetRequiredService<ISecuritySeeder>().SeedAsync(cancellationToken);
            foreach (var account in accounts)
            {
                if (environment.IsDevelopment()) logger.LogWarning("İlk kullanım hesabı oluşturuldu: {UserName} / {Password}", account.UserName, account.TemporaryPassword);
                else logger.LogWarning("İlk kullanım hesabı hazırlandı: {UserName}. Başlangıç parolası güvenli ortam yapılandırmasından yönetilmelidir.", account.UserName);
            }
            isReady = true;
            logger.LogInformation("KPI veritabanı kullanıma hazır.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            StatusMessage = SqlServerConnectionResolver.GetInitializationFailureMessage(exception);
            logger.LogWarning(exception, "KPI veritabanı hazırlanamadı. Başlangıç işlemi 15 saniye sonra yeniden denenecek.");
        }
    }
}
