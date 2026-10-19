using KpiEvaluation.Domain;
using KpiEvaluation.Application;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace KpiEvaluation.Infrastructure;

public sealed class KpiDbContext(DbContextOptions<KpiDbContext> options, ICurrentUser? currentUser = null) : DbContext(options)
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
    public DbSet<EvaluationEmployeeResponse> EvaluationEmployeeResponses => Set<EvaluationEmployeeResponse>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<CompetencyMatrixEntry> CompetencyMatrixEntries => Set<CompetencyMatrixEntry>();
    public DbSet<DevelopmentItem> DevelopmentItems => Set<DevelopmentItem>();
    public DbSet<DevelopmentCheckIn> DevelopmentCheckIns => Set<DevelopmentCheckIn>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(KpiDbContext).Assembly);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries<AuditEntry>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
            throw new ValidationException("Denetim kayıtları değiştirilemez veya silinemez.");
        var changes = ChangeTracker.Entries().Where(x => x.Entity is not AuditEntry
            && x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(x => new { Entry = x, Action = x.State.ToString(), Fields = x.Properties
                .Where(p => x.State != EntityState.Modified || p.IsModified)
                .Select(p => p.Metadata.Name).Where(name => name != nameof(AppUser.PasswordHash)).ToArray() }).ToList();
        if (changes.Count == 0) return await base.SaveChangesAsync(cancellationToken);
        await using var transaction = Database.CurrentTransaction is null ? await Database.BeginTransactionAsync(cancellationToken) : null;
        var count = await base.SaveChangesAsync(cancellationToken);
        foreach (var change in changes)
        {
            var keys = change.Entry.Metadata.FindPrimaryKey()!.Properties
                .Select(p => change.Entry.Property(p.Name).CurrentValue?.ToString() ?? "null");
            AuditEntries.Add(new AuditEntry { UserId = currentUser?.UserId, EntityType = change.Entry.Metadata.ClrType.Name,
                EntityId = string.Join(":", keys), Action = change.Action, ChangedFieldsJson = JsonSerializer.Serialize(change.Fields) });
        }
        await base.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return count;
    }
}
