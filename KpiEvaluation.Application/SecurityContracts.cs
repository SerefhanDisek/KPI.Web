using KpiEvaluation.Domain;

namespace KpiEvaluation.Application;

public interface ICurrentUser
{
    int? UserId { get; }
    bool IsAuthenticated { get; }
}

public interface IAuthorizedRequest
{
    AppPermission Permission { get; }
}

public interface IAnyAuthorizedRequest
{
    IReadOnlyCollection<AppPermission> Permissions { get; }
}

public interface IAllAuthorizedRequest
{
    IReadOnlyCollection<AppPermission> Permissions { get; }
}

public sealed record AuthenticatedUserDto(int Id, string UserName, string DisplayName, int RoleId, string RoleName, string RoleCode, int? EmployeeId,
    bool MustChangePassword, IReadOnlyList<AppPermission> Permissions);
public sealed record UserDto(int Id, string UserName, string DisplayName, int RoleId, string RoleName, string RoleCode, int? EmployeeId,
    string? EmployeeName, bool IsActive, bool MustChangePassword);
public sealed record UserSaveInput(int? Id, string UserName, string DisplayName, int RoleId, int? EmployeeId,
    bool IsActive, string? Password);
public sealed record RoleDto(int Id, string Name, string Code, bool IsSystem, bool IsActive);
public sealed record RolePermissionDto(int RoleId, AppPermission Permission, bool IsAllowed);
public sealed record EmployeeManagerDto(int EmployeeId, string EmployeeName, int? FirstManagerUserId,
    string? FirstManagerName, int? SecondManagerUserId, string? SecondManagerName);
public sealed record ManagerDashboardDto(int UserId, string ManagerName, string RoleCode, string RoleName, int EmployeeCount,
    int ApprovedEvaluationCount, int PendingEvaluationCount, decimal AveragePercentage);

public sealed class AuthorizationException(string message = "Bu işlem için yetkiniz bulunmuyor.") : Exception(message);
