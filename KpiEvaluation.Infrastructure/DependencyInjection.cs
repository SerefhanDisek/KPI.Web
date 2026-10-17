using KpiEvaluation.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KpiEvaluation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string? bootstrapPassword = null)
    {
        services.AddDbContext<KpiDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IProfessionService, ProfessionService>();
        services.AddScoped<IKpiService, KpiService>();
        services.AddScoped<ICompanyGoalService, CompanyGoalService>();
        services.AddScoped<IEvaluationService, EvaluationService>();
        services.AddScoped<IEvaluationImportService, EvaluationImportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEvaluationPeriodService, EvaluationPeriodService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAccessControlService, AccessControlService>();
        services.AddScoped<IPageDefinitionService, PageDefinitionService>();
        services.AddScoped<ISecuritySeeder, SecuritySeeder>();
        services.AddSingleton(new SecurityBootstrapOptions(bootstrapPassword));

        return services;
    }
}
