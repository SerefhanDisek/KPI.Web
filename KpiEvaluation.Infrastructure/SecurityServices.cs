using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using KpiEvaluation.Infrastructure.Repositories;

namespace KpiEvaluation.Infrastructure;

public sealed record SecurityBootstrapAccount(string UserName, string DisplayName, string TemporaryPassword);
public interface ISecuritySeeder
{
    Task<IReadOnlyList<SecurityBootstrapAccount>> SeedAsync(CancellationToken cancellationToken = default);
}
public sealed record SecurityBootstrapOptions(string? Password, bool RequireConfiguredPassword = false);

internal static class PasswordHashing
{
    private const int Iterations = 120_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encoded)
    {
        var parts = encoded.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }
}

public sealed class SecuritySeeder(KpiDbContext db, SecurityBootstrapOptions options) : ISecuritySeeder
{
    public async Task<IReadOnlyList<SecurityBootstrapAccount>> SeedAsync(CancellationToken ct = default)
    {
        await AddRoleAsync("Sistem Yöneticisi", RoleCodes.Administrator, true, ct);
        await AddRoleAsync("1. Amir", RoleCodes.FirstManager, true, ct);
        await AddRoleAsync("2. Amir", RoleCodes.SecondManager, true, ct);
        await AddRoleAsync("Çalışan", RoleCodes.Employee, true, ct);
        await AddRoleAsync("İnsan Kaynakları", RoleCodes.HumanResources, false, ct);
        await db.SaveChangesAsync(ct);
        var roles = await db.AppRoles.ToDictionaryAsync(x => x.Code, ct);
        foreach (var role in roles.Values)
        foreach (var permission in Enum.GetValues<AppPermission>())
        {
            var existing = await db.RolePermissions.SingleOrDefaultAsync(x => x.RoleId == role.Id && x.Permission == permission, ct);
            if (existing is not null)
            {
                if (role.Code == RoleCodes.Administrator) existing.IsAllowed = true;
                continue;
            }
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, Permission = permission, IsAllowed = DefaultAllowed(role.Code, permission) });
        }
        var created = new List<SecurityBootstrapAccount>();
        await AddUserAsync("admin", "Sistem Yöneticisi", roles[RoleCodes.Administrator].Id, created, ct);
        await AddUserAsync("deniz.ozyurek", "Deniz Özyürek", roles[RoleCodes.FirstManager].Id, created, ct);
        await AddUserAsync("melih.yurtsever", "Melih Yurtsever", roles[RoleCodes.SecondManager].Id, created, ct);
        await AddUserAsync("kader.guveren.disek", "Kader Güveren Dişek", roles[RoleCodes.HumanResources].Id, created, ct);
        await db.SaveChangesAsync(ct);
        var firstManagerId = await db.AppUsers.Where(x => x.UserName == "deniz.ozyurek").Select(x => x.Id).SingleAsync(ct);
        var secondManagerId = await db.AppUsers.Where(x => x.UserName == "melih.yurtsever").Select(x => x.Id).SingleAsync(ct);
        await db.Employees.Where(x => x.FirstManagerUserId == null).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.FirstManagerUserId, firstManagerId), ct);
        await db.Employees.Where(x => x.SecondManagerUserId == null).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.SecondManagerUserId, secondManagerId), ct);
        return created;
    }

    private async Task AddRoleAsync(string name, string code, bool isSystem, CancellationToken ct)
    {
        if (await db.AppRoles.AnyAsync(x => x.Code == code, ct)) return;
        db.AppRoles.Add(new AppRole { Name = name, Code = code, IsSystem = isSystem });
    }

    private async Task AddUserAsync(string userName, string displayName, int roleId,
        ICollection<SecurityBootstrapAccount> created, CancellationToken ct)
    {
        var existing = await db.AppUsers.SingleOrDefaultAsync(x => x.UserName == userName, ct);
        if (existing is not null) return;
        if (options.RequireConfiguredPassword && string.IsNullOrWhiteSpace(options.Password))
            throw new ValidationException("Üretimde ilk hesapları oluşturmak için Authentication:BootstrapPassword güvenli ortam yapılandırmasında tanımlanmalıdır.");
        var password = string.IsNullOrWhiteSpace(options.Password) ? CreatePassword() : options.Password;
        AuthenticationRepository.ValidatePassword(password);
        db.AppUsers.Add(new AppUser
        {
            UserName = userName,
            DisplayName = displayName,
            RoleId = roleId,
            PasswordHash = PasswordHashing.Hash(password),
            MustChangePassword = false
        });
        created.Add(new(userName, displayName, password));
    }

    private static bool DefaultAllowed(string roleCode, AppPermission permission) => roleCode switch
    {
        RoleCodes.Administrator => true,
        RoleCodes.FirstManager or RoleCodes.SecondManager => permission is AppPermission.DashboardView
            or AppPermission.EvaluationCreate or AppPermission.EvaluationApprove or AppPermission.EvaluationHistory,
        RoleCodes.Employee or RoleCodes.HumanResources => permission is AppPermission.DashboardView or AppPermission.EvaluationHistory,
        _ => false
    };

    private static string CreatePassword() => $"Kp!{Convert.ToHexString(RandomNumberGenerator.GetBytes(7))}a";
}
