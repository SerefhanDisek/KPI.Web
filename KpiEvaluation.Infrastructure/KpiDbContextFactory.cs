using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KpiEvaluation.Infrastructure;

public sealed class KpiDbContextFactory : IDesignTimeDbContextFactory<KpiDbContext>
{
    public KpiDbContext CreateDbContext(string[] args)
    {
        // Yalnızca EF migration tasarımı içindir; web uygulaması çalışma zamanı bağlantısını yapılandırmadan alır.
        var connectionString = Environment.GetEnvironmentVariable("KPI_EVALUATION_DESIGN_CONNECTION")
            ?? "Server=.;Database=kpi_evaluation_db;Integrated Security=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<KpiDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new KpiDbContext(options);
    }
}
