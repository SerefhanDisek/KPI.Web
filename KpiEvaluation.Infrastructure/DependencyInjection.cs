using KpiEvaluation.Application.Repositories;
using KpiEvaluation.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KpiEvaluation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string? bootstrapPassword = null)
    {
        services.AddDbContext<KpiDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeLeaveRepository, EmployeeLeaveRepository>();
        services.AddScoped<IProfessionRepository, ProfessionRepository>();
        services.AddScoped<IKpiRepository, KpiRepository>();
        services.AddScoped<ICompanyGoalRepository, CompanyGoalRepository>();
        services.AddScoped<IEvaluationRepository, EvaluationRepository>();
        services.AddScoped<IEvaluationImportRepository, EvaluationImportRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IEvaluationPeriodRepository, EvaluationPeriodRepository>();
        services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
        services.AddScoped<IAccessControlRepository, AccessControlRepository>();
        services.AddScoped<IPageDefinitionRepository, PageDefinitionRepository>();
        services.AddScoped<ISecuritySeeder, SecuritySeeder>();
        services.AddSingleton(new SecurityBootstrapOptions(bootstrapPassword));

        return services;
    }
}
