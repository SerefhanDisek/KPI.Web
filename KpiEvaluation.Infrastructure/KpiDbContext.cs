using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;

namespace KpiEvaluation.Infrastructure;

public sealed class KpiDbContext(DbContextOptions<KpiDbContext> options) : DbContext(options)
{
    public DbSet<Profession> Professions => Set<Profession>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<KpiCriterion> KpiCriteria => Set<KpiCriterion>();
    public DbSet<CompanyGoal> CompanyGoals => Set<CompanyGoal>();
    public DbSet<EvaluationPeriod> EvaluationPeriods => Set<EvaluationPeriod>();
    public DbSet<EmployeeEvaluation> EmployeeEvaluations => Set<EmployeeEvaluation>();
    public DbSet<EmployeeEvaluationDetail> EmployeeEvaluationDetails => Set<EmployeeEvaluationDetail>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<PageDefinition> PageDefinitions => Set<PageDefinition>();
    public DbSet<EmployeeLeave> EmployeeLeaves => Set<EmployeeLeave>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(KpiDbContext).Assembly);
}
