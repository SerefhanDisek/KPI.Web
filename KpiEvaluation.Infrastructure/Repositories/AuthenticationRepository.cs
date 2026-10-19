using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using KpiEvaluation.Application.Repositories;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class AuthenticationRepository(KpiDbContext db) : IAuthenticationRepository
{
    public async Task<AuthenticatedUserDto?> AuthenticateAsync(string userName, string password, CancellationToken ct = default)
    {
        var normalized = userName.Trim().ToLowerInvariant();
        var user = await db.AppUsers.AsNoTracking().Include(x => x.Role).SingleOrDefaultAsync(x => x.UserName == normalized && x.IsActive && x.Role.IsActive, ct);
        if (user is null || !PasswordHashing.Verify(password, user.PasswordHash)) return null;
        var permissions = user.Role.Code == RoleCodes.Administrator ? Enum.GetValues<AppPermission>() : await GetPermissionsAsync(user.RoleId, ct);
        return new(user.Id, user.UserName, user.DisplayName, user.RoleId, user.Role.Name, user.Role.Code, user.EmployeeId, false, permissions);
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await db.AppUsers.SingleOrDefaultAsync(x => x.Id == userId && x.IsActive && x.Role.IsActive, ct)
            ?? throw new ValidationException("Kullanıcı bulunamadı.");
        if (!PasswordHashing.Verify(currentPassword, user.PasswordHash)) throw new ValidationException("Mevcut parola hatalı.");
        ValidatePassword(newPassword);
        user.PasswordHash = PasswordHashing.Hash(newPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyList<AppPermission>> GetPermissionsAsync(int roleId, CancellationToken ct) =>
        await db.RolePermissions.AsNoTracking().Where(x => x.RoleId == roleId && x.IsAllowed).Select(x => x.Permission).ToListAsync(ct);

    internal static void ValidatePassword(string password)
    {
        if (password.Length < 10 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || password.All(char.IsLetterOrDigit))
            throw new ValidationException("Parola en az 10 karakter olmalı; büyük harf, küçük harf, rakam ve özel karakter içermelidir.");
    }
}
