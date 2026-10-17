using KpiEvaluation.Infrastructure;

namespace KPI.Web.Infrastructure;

public static class DatabaseStartup
{
    public static async Task TryInitializeAsync(IServiceProvider services, ILogger logger)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
            var accounts = await scope.ServiceProvider.GetRequiredService<ISecuritySeeder>().SeedAsync();
            foreach (var account in accounts)
                logger.LogWarning("İlk kullanım hesabı oluşturuldu: {UserName} / {Password}", account.UserName, account.TemporaryPassword);
            logger.LogInformation("KPI veritabanı kullanıma hazır.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "KPI veritabanı başlangıçta hazırlanamadı. Uygulama açık kalacak ve bağlantı sonraki isteklerde yeniden denenebilecek.");
        }
    }
}
