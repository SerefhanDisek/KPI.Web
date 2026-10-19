using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using KpiEvaluation.Application.Repositories;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class AccessControlRepository(KpiDbContext db) : IAccessControlRepository
{
    public async Task<bool> HasPermissionAsync(int userId, AppPermission permission, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(permission)) return false;
        var user = await db.AppUsers.AsNoTracking().Where(x => x.Id == userId && x.IsActive && x.Role.IsActive)
            .Select(x => new { x.RoleId, RoleCode = x.Role.Code }).SingleOrDefaultAsync(ct);
        return user is not null && (user.RoleCode == RoleCodes.Administrator || await db.RolePermissions.AsNoTracking()
            .AnyAsync(x => x.RoleId == user.RoleId && x.Permission == permission && x.IsAllowed, ct));
    }

    public async Task<AuthenticatedUserDto> GetCurrentAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.AppUsers.AsNoTracking().Include(x => x.Role).SingleOrDefaultAsync(x => x.Id == userId && x.IsActive && x.Role.IsActive, ct)
            ?? throw new AuthorizationException();
        IReadOnlyList<AppPermission> permissions = user.Role.Code == RoleCodes.Administrator ? Enum.GetValues<AppPermission>()
            : await db.RolePermissions.AsNoTracking().Where(x => x.RoleId == user.RoleId && x.IsAllowed).Select(x => x.Permission).ToListAsync(ct);
        return new(user.Id, user.UserName, user.DisplayName, user.RoleId, user.Role.Name, user.Role.Code, user.EmployeeId, false, permissions);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default) =>
        await db.AppUsers.AsNoTracking().OrderBy(x => x.DisplayName)
            .Select(x => new UserDto(x.Id, x.UserName, x.DisplayName, x.RoleId, x.Role.Name, x.Role.Code, x.EmployeeId,
                x.Employee == null ? null : x.Employee.FullName, x.IsActive, false)).ToListAsync(ct);

    public async Task<UserDto> SaveUserAsync(UserSaveInput input, CancellationToken ct = default)
    {
        var userName = input.UserName.Trim().ToLowerInvariant();
        var displayName = input.DisplayName.Trim();
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(displayName)) throw new ValidationException("Kullanıcı adı ve görünen ad zorunludur.");
        if (!Regex.IsMatch(userName, "^[a-z0-9._-]+$")) throw new ValidationException("Kullanıcı adı yalnızca küçük harf, rakam, nokta, tire ve alt çizgi içerebilir.");
        if (await db.AppUsers.AnyAsync(x => x.UserName == userName && x.Id != input.Id, ct)) throw new ValidationException("Bu kullanıcı adı zaten kullanılıyor.");
        if (!await db.AppRoles.AnyAsync(x => x.Id == input.RoleId && x.IsActive, ct)) throw new ValidationException("Geçerli bir rol seçmelisiniz.");
        if (input.EmployeeId is not null && !await db.Employees.AnyAsync(x => x.Id == input.EmployeeId && x.IsActive, ct)) throw new ValidationException("Çalışan bulunamadı.");
        if (input.EmployeeId is not null && await db.AppUsers.AnyAsync(x => x.EmployeeId == input.EmployeeId && x.Id != input.Id, ct)) throw new ValidationException("Bu çalışana ait bir kullanıcı hesabı zaten var.");

        AppUser user;
        if (input.Id is null)
        {
            if (string.IsNullOrWhiteSpace(input.Password)) throw new ValidationException("Yeni kullanıcı için parola zorunludur.");
            AuthenticationRepository.ValidatePassword(input.Password);
            user = new AppUser { UserName = userName, DisplayName = displayName, PasswordHash = PasswordHashing.Hash(input.Password), MustChangePassword = false };
            db.AppUsers.Add(user);
        }
        else
        {
            user = await db.AppUsers.FindAsync([input.Id.Value], ct) ?? throw new ValidationException("Kullanıcı bulunamadı.");
            if (!string.IsNullOrWhiteSpace(input.Password))
            {
                AuthenticationRepository.ValidatePassword(input.Password);
                user.PasswordHash = PasswordHashing.Hash(input.Password);
                user.MustChangePassword = false;
            }
        }
        user.UserName = userName; user.DisplayName = displayName; user.RoleId = input.RoleId; user.EmployeeId = input.EmployeeId;
        user.IsActive = input.IsActive; user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        var role = await db.AppRoles.AsNoTracking().SingleAsync(x => x.Id == user.RoleId, ct);
        return new(user.Id, user.UserName, user.DisplayName, user.RoleId, role.Name, role.Code, user.EmployeeId,
            input.EmployeeId is null ? null : await db.Employees.Where(x => x.Id == input.EmployeeId).Select(x => x.FullName).SingleAsync(ct), user.IsActive, false);
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default) =>
        await db.AppRoles.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new RoleDto(x.Id, x.Name, x.Code, x.IsSystem, x.IsActive)).ToListAsync(ct);

    public async Task<RoleDto> SaveRoleAsync(int? id, string name, string code, bool isActive, CancellationToken ct = default)
    {
        name = name.Trim(); code = code.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(name) || !Regex.IsMatch(code, "^[a-z0-9-]+$")) throw new ValidationException("Rol adı zorunludur; rol kodu küçük harf, rakam ve tire içerebilir.");
        if (await db.AppRoles.AnyAsync(x => (x.Name == name || x.Code == code) && x.Id != id, ct)) throw new ValidationException("Rol adı veya kodu zaten kullanılıyor.");
        AppRole role;
        if (id is null) { role = new AppRole { Name = name, Code = code }; db.AppRoles.Add(role); }
        else
        {
            role = await db.AppRoles.FindAsync([id.Value], ct) ?? throw new ValidationException("Rol bulunamadı.");
            if (role.IsSystem && role.Code != code) throw new ValidationException("Sistem rolünün kodu değiştirilemez.");
        }
        role.Name = name; role.Code = code; role.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        foreach (var permission in Enum.GetValues<AppPermission>())
            if (!await db.RolePermissions.AnyAsync(x => x.RoleId == role.Id && x.Permission == permission, ct))
                db.RolePermissions.Add(new RolePermission { RoleId = role.Id, Permission = permission, IsAllowed = false });
        await db.SaveChangesAsync(ct);
        return new(role.Id, role.Name, role.Code, role.IsSystem, role.IsActive);
    }

    public async Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(CancellationToken ct = default)
    {
        var roles = await db.AppRoles.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Code }).ToListAsync(ct);
        var rows = await db.RolePermissions.AsNoTracking().ToListAsync(ct);
        return roles.SelectMany(role => Enum.GetValues<AppPermission>().Select(permission => new RolePermissionDto(role.Id, permission,
            role.Code == RoleCodes.Administrator || rows.Any(x => x.RoleId == role.Id && x.Permission == permission && x.IsAllowed)))).ToList();
    }

    public async Task SaveRolePermissionsAsync(int roleId, IReadOnlyList<AppPermission> permissions, CancellationToken ct = default)
    {
        var roleCode = await db.AppRoles.Where(x => x.Id == roleId && x.IsActive).Select(x => x.Code).SingleOrDefaultAsync(ct)
            ?? throw new ValidationException("Rol bulunamadı.");
        var selected = (roleCode == RoleCodes.Administrator ? Enum.GetValues<AppPermission>() : permissions).ToHashSet();
        var rows = await db.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(ct);
        foreach (var permission in Enum.GetValues<AppPermission>())
        {
            var row = rows.SingleOrDefault(x => x.Permission == permission);
            if (row is null) db.RolePermissions.Add(new RolePermission { RoleId = roleId, Permission = permission, IsAllowed = selected.Contains(permission) });
            else row.IsAllowed = selected.Contains(permission);
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EmployeeManagerDto>> GetEmployeeManagersAsync(CancellationToken ct = default) =>
        await db.Employees.AsNoTracking().OrderBy(x => x.FullName).Select(x => new EmployeeManagerDto(x.Id, x.FullName,
            x.FirstManagerUserId, x.FirstManagerUser == null ? null : x.FirstManagerUser.DisplayName,
            x.SecondManagerUserId, x.SecondManagerUser == null ? null : x.SecondManagerUser.DisplayName)).ToListAsync(ct);

    public async Task SaveEmployeeManagersAsync(int employeeId, int? firstManagerUserId, int? secondManagerUserId, CancellationToken ct = default)
    {
        if (firstManagerUserId == secondManagerUserId && firstManagerUserId is not null) throw new ValidationException("1. ve 2. amir farklı kullanıcılar olmalıdır.");
        var employee = await db.Employees.FindAsync([employeeId], ct) ?? throw new ValidationException("Çalışan bulunamadı.");
        if (firstManagerUserId is not null && !await db.AppUsers.AnyAsync(x => x.Id == firstManagerUserId && x.IsActive && x.Role.Code == RoleCodes.FirstManager, ct)) throw new ValidationException("Geçerli bir 1. amir seçmelisiniz.");
        if (secondManagerUserId is not null && !await db.AppUsers.AnyAsync(x => x.Id == secondManagerUserId && x.IsActive && x.Role.Code == RoleCodes.SecondManager, ct)) throw new ValidationException("Geçerli bir 2. amir seçmelisiniz.");
        employee.FirstManagerUserId = firstManagerUserId; employee.SecondManagerUserId = secondManagerUserId; employee.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ManagerDashboardDto>> GetManagerDashboardAsync(int? periodId, CancellationToken ct = default)
    {
        var managers = await db.AppUsers.AsNoTracking().Where(x => x.IsActive && (x.Role.Code == RoleCodes.FirstManager || x.Role.Code == RoleCodes.SecondManager))
            .Select(x => new { x.Id, x.DisplayName, RoleCode=x.Role.Code, RoleName=x.Role.Name }).ToListAsync(ct);
        var result = new List<ManagerDashboardDto>();
        foreach (var manager in managers)
        {
            var employeeIds = db.Employees.AsNoTracking().Where(x => x.IsActive &&
                (x.FirstManagerUserId == manager.Id || x.SecondManagerUserId == manager.Id)).Select(x => x.Id);
            var evaluations = db.EmployeeEvaluations.AsNoTracking().Where(x => x.EvaluatorUserId == manager.Id &&
                (periodId == null || x.EvaluationPeriodId == periodId));
            var approved = await evaluations.Where(x => x.Status == EvaluationStatus.Approved).ToListAsync(ct);
            result.Add(new(manager.Id, manager.DisplayName, manager.RoleCode, manager.RoleName, await employeeIds.CountAsync(ct), approved.Count,
                await evaluations.CountAsync(x => x.Status != EvaluationStatus.Approved, ct), approved.Count == 0 ? 0 : Math.Round(approved.Average(x => x.ScorePercentage), 2)));
        }
        return result.OrderBy(x => x.RoleCode).ThenBy(x => x.ManagerName).ToList();
    }
}
