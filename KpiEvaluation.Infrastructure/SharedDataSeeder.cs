using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using KpiEvaluation.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KpiEvaluation.Infrastructure;

public interface ISharedDataSeeder
{
    Task<IReadOnlyList<SecurityBootstrapAccount>> SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class SharedDataSeeder(KpiDbContext db, SecurityBootstrapOptions bootstrap) : ISharedDataSeeder
{
    public const string Version = "shared-data-20261112-v1";
    public const string NewDatabaseMarker = Version + ":new-database";
    public const string FileName = "08_shared_data_20261112.json";
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private readonly List<SecurityBootstrapAccount> accounts = [];
    private readonly HashSet<int> updatedRoles = [];
    private readonly HashSet<int> updatedUsers = [];
    private readonly HashSet<(int, AppPermission)> updatedPermissions = [];
    private readonly Dictionary<int, string> professionOrigins = [];
    private List<AppRole> roles = [];
    private List<RolePermission> permissions = [];
    private List<AppUser> users = [];
    private List<Profession> professions = [];
    private List<Employee> employees = [];
    private List<EvaluationPeriod> periods = [];
    private List<CompanyGoal> goals = [];
    private List<KpiCriterion> kpis = [];
    private List<KpiCriterionProfession> scopes = [];
    private List<EmployeeEvaluation> evaluations = [];
    private List<EvaluationEmployeeResponse> responses = [];
    private List<EmployeeLeave> leaves = [];
    private List<EmployeeLeaveBalance> balances = [];
    private HashSet<string> auditKeys = [];

    public async Task<IReadOnlyList<SecurityBootstrapAccount>> SeedAsync(CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        if (await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM __KpiSeedHistory WHERE ScriptName = {Version}").SingleAsync(ct) != 0)
            return [];
        var path = Path.Combine(AppContext.BaseDirectory, "Database", FileName);
        var package = JsonSerializer.Deserialize<SeedPackage>(await File.ReadAllTextAsync(path, ct))
            ?? throw new InvalidDataException("Paylaşılan başlangıç veri paketi okunamadı.");
        if (package.Version != Version || package.Sources.Select(x => x.Name).Distinct().Count() != package.Sources.Length)
            throw new InvalidDataException("Paylaşılan başlangıç veri paketinin sürümü geçersiz.");
        var fresh = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM __KpiSeedHistory WHERE ScriptName = {NewDatabaseMarker}").SingleAsync(ct) != 0;
        roles = await db.AppRoles.ToListAsync(ct);
        permissions = await db.RolePermissions.ToListAsync(ct);
        users = await db.AppUsers.ToListAsync(ct);
        professions = await db.Professions.ToListAsync(ct);
        employees = await db.Employees.ToListAsync(ct);
        periods = await db.EvaluationPeriods.ToListAsync(ct);
        goals = await db.CompanyGoals.ToListAsync(ct);
        kpis = await db.KpiCriteria.ToListAsync(ct);
        scopes = await db.Set<KpiCriterionProfession>().ToListAsync(ct);
        evaluations = await db.EmployeeEvaluations.ToListAsync(ct);
        responses = await db.EvaluationEmployeeResponses.ToListAsync(ct);
        leaves = await db.EmployeeLeaves.ToListAsync(ct);
        balances = await db.EmployeeLeaveBalances.ToListAsync(ct);
        auditKeys = (await db.AuditEntries.AsNoTracking().ToListAsync(ct)).Select(AuditKey).ToHashSet();
        foreach (var source in package.Sources) await ImportSourceAsync(source, fresh, ct);
        await db.SaveChangesAsync(ct);
        foreach (var legacy in new[] { "03_seed_professions.sql", "04_seed_employees.sql", "05_seed_kpi_criteria.sql", "06_seed_evaluation_period.sql", "07_seed_strategic_kpi_data.sql" })
            await db.Database.ExecuteSqlInterpolatedAsync($"IF NOT EXISTS (SELECT 1 FROM __KpiSeedHistory WHERE ScriptName={legacy}) INSERT INTO __KpiSeedHistory(ScriptName) VALUES({legacy})", ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO __KpiSeedHistory(ScriptName) VALUES({Version})", ct);
        await transaction.CommitAsync(ct);
        return accounts;
    }

    private async Task ImportSourceAsync(SeedSource source, bool fresh, CancellationToken ct)
    {
        var map = new SourceMap();
        foreach (var item in source.Rows<AppRole>("app_role"))
        {
            var sourceId = item.Id;
            var target = Unique(roles.Where(x => string.Equals(x.Code, item.Code, StringComparison.OrdinalIgnoreCase)), "rol");
            if (target is null)
            {
                item.Id = 0; target = item; roles.Add(target); db.AppRoles.Add(target);
            }
            else if (fresh && !updatedRoles.Contains(target.Id))
            {
                target.Name = item.Name; target.IsSystem = item.IsSystem; target.IsActive = item.IsActive; target.CreatedAt = item.CreatedAt;
            }
            await db.SaveChangesAsync(ct);
            updatedRoles.Add(target.Id); map.Roles[sourceId] = target.Id;
        }
        foreach (var item in source.Rows<RolePermission>("role_permission"))
        {
            item.RoleId = Id(map.Roles, item.RoleId);
            if (roles.Single(x => x.Id == item.RoleId).Code == RoleCodes.Administrator) item.IsAllowed = true;
            var target = permissions.SingleOrDefault(x => x.RoleId == item.RoleId && x.Permission == item.Permission);
            if (target is null) { permissions.Add(item); db.RolePermissions.Add(item); updatedPermissions.Add((item.RoleId, item.Permission)); }
            else if (fresh && updatedPermissions.Add((item.RoleId, item.Permission))) target.IsAllowed = item.IsAllowed;
        }
        await db.SaveChangesAsync(ct);
        var sourceUsers = source.Rows<SeedUser>("app_user");
        foreach (var item in sourceUsers)
        {
            var target = Unique(users.Where(x => string.Equals(x.UserName, item.UserName, StringComparison.OrdinalIgnoreCase)), "kullanıcı");
            if (target is null)
            {
                if (bootstrap.RequireConfiguredPassword && string.IsNullOrWhiteSpace(bootstrap.Password))
                    throw new ValidationException("Üretimde yeni hesaplar için Authentication:BootstrapPassword yapılandırılmalıdır.");
                var password = string.IsNullOrWhiteSpace(bootstrap.Password) ? $"Kp!{Convert.ToHexString(RandomNumberGenerator.GetBytes(7))}a" : bootstrap.Password;
                AuthenticationRepository.ValidatePassword(password);
                target = new AppUser { UserName = item.UserName, DisplayName = item.DisplayName, PasswordHash = PasswordHashing.Hash(password),
                    RoleId = Id(map.Roles, item.RoleId), IsActive = item.IsActive, CreatedAt = item.CreatedAt, UpdatedAt = item.UpdatedAt };
                db.AppUsers.Add(target); users.Add(target); await db.SaveChangesAsync(ct);
                accounts.Add(new(item.UserName, item.DisplayName, password)); map.LinkedUsers.Add(item.Id);
            }
            else if (fresh && !updatedUsers.Contains(target.Id))
            {
                target.DisplayName = item.DisplayName; target.RoleId = Id(map.Roles, item.RoleId); target.IsActive = item.IsActive;
                target.CreatedAt = item.CreatedAt; target.UpdatedAt = item.UpdatedAt;
                map.LinkedUsers.Add(item.Id);
            }
            updatedUsers.Add(target.Id); map.Users[item.Id] = target.Id;
        }
        await db.SaveChangesAsync(ct);
        foreach (var item in source.Rows<Profession>("profession"))
        {
            var sourceId = item.Id;
            var target = Unique(professions.Where(x => Same(x.Name, item.Name)), "meslek");
            if (target is null)
            {
                item.Id = 0; item.FirstManagerUserId = OptionalId(map.Users, item.FirstManagerUserId); item.SecondManagerUserId = OptionalId(map.Users, item.SecondManagerUserId);
                target = item; db.Professions.Add(target); professions.Add(target); await db.SaveChangesAsync(ct);
                professionOrigins[target.Id] = source.Name; map.NewProfessions.Add(target.Id);
            }
            map.Professions[sourceId] = target.Id;
        }
        foreach (var item in source.Rows<TeamManagerAssignment>("team_manager_assignment"))
        {
            item.ProfessionId = Id(map.Professions, item.ProfessionId); item.ManagerUserId = Id(map.Users, item.ManagerUserId);
            if (map.NewProfessions.Contains(item.ProfessionId)) db.TeamManagerAssignments.Add(item);
        }
        await db.SaveChangesAsync(ct);
        foreach (var item in source.Rows<Employee>("employee"))
        {
            var sourceId = item.Id; var professionId = Id(map.Professions, item.ProfessionId);
            var candidates = employees.Where(x => Same(x.FullName, item.FullName)).ToArray();
            var target = candidates.Length == 1 ? candidates[0] : Unique(candidates.Where(x => x.ProfessionId == professionId), "çalışan");
            if (target is null)
            {
                item.Id = 0; item.ProfessionId = professionId;
                var team = professions.Single(x => x.Id == professionId);
                if (professionOrigins.GetValueOrDefault(professionId) == source.Name)
                { item.FirstManagerUserId = OptionalId(map.Users, item.FirstManagerUserId); item.SecondManagerUserId = OptionalId(map.Users, item.SecondManagerUserId); }
                else { item.FirstManagerUserId = team.FirstManagerUserId; item.SecondManagerUserId = team.SecondManagerUserId; }
                target = item; db.Employees.Add(target); employees.Add(target); await db.SaveChangesAsync(ct);
            }
            map.Employees[sourceId] = target.Id;
        }
        foreach (var item in sourceUsers.Where(x => x.EmployeeId is not null && map.LinkedUsers.Contains(x.Id)))
        {
            var employeeId = Id(map.Employees, item.EmployeeId!.Value); var user = users.Single(x => x.Id == Id(map.Users, item.Id));
            if (users.Any(x => x.Id != user.Id && x.EmployeeId == employeeId)) throw new InvalidDataException("Başlangıç verisindeki çalışan hesabı bağlantısı belirsiz.");
            user.EmployeeId = employeeId;
        }
        await db.SaveChangesAsync(ct);
        foreach (var item in source.Rows<EvaluationPeriod>("evaluation_period"))
        {
            var sourceId = item.Id; var target = Unique(periods.Where(x => Same(x.Name, item.Name)), "dönem");
            if (target is null) { item.Id = 0; target = item; periods.Add(target); db.EvaluationPeriods.Add(target); await db.SaveChangesAsync(ct); }
            map.Periods[sourceId] = target.Id;
        }
        foreach (var item in source.Rows<CompanyGoal>("company_goal"))
        {
            var sourceId = item.Id; item.EvaluationPeriodId = OptionalId(map.Periods, item.EvaluationPeriodId);
            var target = Unique(goals.Where(x => Same(x.Name, item.Name) && x.EvaluationPeriodId == item.EvaluationPeriodId), "şirket hedefi");
            if (target is null) { item.Id = 0; target = item; goals.Add(target); db.CompanyGoals.Add(target); await db.SaveChangesAsync(ct); }
            map.Goals[sourceId] = target.Id;
        }
        // Snapshot the candidates before this source so distinct same-named historical questions stay distinct.
        var existingKpis = kpis.ToArray(); var sourceScopes = source.Rows<KpiCriterionProfession>("kpi_criterion_profession");
        foreach (var item in source.Rows<KpiCriterion>("kpi_criterion"))
        {
            var sourceId = item.Id;
            var scopeIds = sourceScopes.Where(x => x.KpiCriterionId == sourceId).Select(x => Id(map.Professions, x.ProfessionId))
                .Concat(item.ProfessionId is { } primary ? [Id(map.Professions, primary)] : []).ToHashSet();
            var candidates = existingKpis.Where(x => Same(CleanKpiTitle(x.Title), CleanKpiTitle(item.Title)) && x.IsCommon == item.IsCommon && scopeIds.SetEquals(
                scopes.Where(p => p.KpiCriterionId == x.Id).Select(p => p.ProfessionId).Concat(x.ProfessionId is { } id ? [id] : []))).ToArray();
            var target = candidates.Length > 1 ? Unique(candidates.Where(x => x.CreatedAt == item.CreatedAt), "KPI") : candidates.SingleOrDefault();
            if (target is null)
            {
                item.Id = 0; item.Title = CleanKpiTitle(item.Title); item.ProfessionId = OptionalId(map.Professions, item.ProfessionId); item.CompanyGoalId = OptionalId(map.Goals, item.CompanyGoalId);
                target = item; kpis.Add(target); db.KpiCriteria.Add(target); await db.SaveChangesAsync(ct);
                foreach (var link in sourceScopes.Where(x => x.KpiCriterionId == sourceId))
                { var scope = new KpiCriterionProfession { KpiCriterionId = target.Id, ProfessionId = Id(map.Professions, link.ProfessionId) }; scopes.Add(scope); db.Set<KpiCriterionProfession>().Add(scope); }
                await db.SaveChangesAsync(ct);
            }
            map.Kpis[sourceId] = target.Id;
        }
        foreach (var item in source.Rows<EmployeeEvaluation>("employee_evaluation"))
        {
            var sourceId = item.Id; item.EmployeeId = Id(map.Employees, item.EmployeeId); item.EvaluationPeriodId = Id(map.Periods, item.EvaluationPeriodId);
            item.EvaluatorUserId = OptionalId(map.Users, item.EvaluatorUserId);
            var target = Unique(evaluations.Where(x => x.EmployeeId == item.EmployeeId && x.EvaluationPeriodId == item.EvaluationPeriodId && x.EvaluatorUserId == item.EvaluatorUserId), "değerlendirme");
            if (target is null)
            {
                item.Id = 0; item.ProfessionIdSnapshot = Id(map.Professions, item.ProfessionIdSnapshot); item.ApprovedByUserId = OptionalId(map.Users, item.ApprovedByUserId);
                target = item; evaluations.Add(target); db.EmployeeEvaluations.Add(target); await db.SaveChangesAsync(ct); map.NewEvaluations.Add(target.Id);
            }
            map.Evaluations[sourceId] = target.Id;
        }
        foreach (var item in source.Rows<EmployeeEvaluationDetail>("employee_evaluation_detail"))
        {
            item.EmployeeEvaluationId = LongId(map.Evaluations, item.EmployeeEvaluationId);
            if (!map.NewEvaluations.Contains(item.EmployeeEvaluationId)) continue;
            item.Id = 0; item.KpiCriterionId = Id(map.Kpis, item.KpiCriterionId); db.EmployeeEvaluationDetails.Add(item);
        }
        await db.SaveChangesAsync(ct);
        foreach (var item in source.Rows<EvaluationEmployeeResponse>("evaluation_employee_response"))
        {
            item.EvaluationId = LongId(map.Evaluations, item.EvaluationId); item.UserId = Id(map.Users, item.UserId);
            if (responses.Any(x => x.EvaluationId == item.EvaluationId && x.UserId == item.UserId && x.CreatedAt == item.CreatedAt && x.Decision == item.Decision && x.Comment == item.Comment)) continue;
            item.Id = 0; responses.Add(item); db.EvaluationEmployeeResponses.Add(item);
        }
        foreach (var item in source.Rows<EmployeeLeave>("employee_leave"))
        {
            item.EmployeeId = Id(map.Employees, item.EmployeeId);
            if (leaves.Any(x => x.EmployeeId == item.EmployeeId && x.StartDate == item.StartDate && x.EndDate == item.EndDate && x.LeaveType == item.LeaveType && x.Note == item.Note)) continue;
            item.Id = 0; item.CreatedByUserId = Id(map.Users, item.CreatedByUserId); item.UpdatedByUserId = OptionalId(map.Users, item.UpdatedByUserId);
            leaves.Add(item); db.EmployeeLeaves.Add(item);
        }
        foreach (var item in source.Rows<EmployeeLeaveBalance>("employee_leave_balance"))
        {
            item.EmployeeId = Id(map.Employees, item.EmployeeId);
            if (balances.Any(x => x.EmployeeId == item.EmployeeId)) continue;
            item.Id = 0; item.CreatedByUserId = Id(map.Users, item.CreatedByUserId); item.UpdatedByUserId = OptionalId(map.Users, item.UpdatedByUserId);
            balances.Add(item); db.EmployeeLeaveBalances.Add(item);
        }
        await db.SaveChangesAsync(ct);
        foreach (var item in source.Rows<AuditEntry>("audit_entry"))
        {
            item.UserId = OptionalId(map.Users, item.UserId);
            if (!auditKeys.Add(AuditKey(item))) continue;
            item.Id = 0; db.AuditEntries.Add(item);
        }
        await db.SaveChangesAsync(ct);
    }

    private static bool Same(string left, string right) => left.Trim().Normalize().ToUpper(Turkish) == right.Trim().Normalize().ToUpper(Turkish);
    private static string CleanKpiTitle(string value) => value.Replace("\uF0B7", "").TrimStart();
    private static T? Unique<T>(IEnumerable<T> candidates, string kind) where T : class
    {
        var matches = candidates.Take(2).ToArray();
        if (matches.Length > 1) throw new InvalidDataException($"Paylaşılan başlangıç verisindeki {kind} eşleştirmesi belirsiz. Mevcut kayıtlar değiştirilmedi.");
        return matches.SingleOrDefault();
    }
    private static int Id(Dictionary<int, int> map, int id) => map.TryGetValue(id, out var target) ? target : throw new InvalidDataException("Başlangıç verisinde eksik ilişkili kayıt var.");
    private static int? OptionalId(Dictionary<int, int> map, int? id) => id is null ? null : Id(map, id.Value);
    private static long LongId(Dictionary<long, long> map, long id) => map.TryGetValue(id, out var target) ? target : throw new InvalidDataException("Başlangıç verisinde eksik değerlendirme var.");
    private static string AuditKey(AuditEntry row) => JsonSerializer.Serialize(new { row.UserId, row.EntityType, row.EntityId, row.Action, row.ChangedFieldsJson, row.CreatedAt });
    private sealed record SeedPackage(string Version, SeedSource[] Sources);
    private sealed record SeedSource(string Name, Dictionary<string, JsonElement[]> Tables)
    {
        public T[] Rows<T>(string name) => Tables.TryGetValue(name, out var rows)
            ? rows.Select(row => row.Deserialize<T>() ?? throw new InvalidDataException("Başlangıç veri satırı okunamadı.")).ToArray()
            : throw new InvalidDataException("Başlangıç veri paketinde eksik tablo var.");
    }
    private sealed record SeedUser(int Id, string UserName, string DisplayName, int RoleId, int? EmployeeId, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);
    private sealed class SourceMap
    {
        public Dictionary<int, int> Roles { get; } = [];
        public Dictionary<int, int> Users { get; } = [];
        public Dictionary<int, int> Professions { get; } = [];
        public Dictionary<int, int> Employees { get; } = [];
        public Dictionary<int, int> Periods { get; } = [];
        public Dictionary<int, int> Goals { get; } = [];
        public Dictionary<int, int> Kpis { get; } = [];
        public Dictionary<long, long> Evaluations { get; } = [];
        public HashSet<int> LinkedUsers { get; } = [];
        public HashSet<int> NewProfessions { get; } = [];
        public HashSet<long> NewEvaluations { get; } = [];
    }
}
