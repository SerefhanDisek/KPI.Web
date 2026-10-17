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

public interface IAuthenticationService
{
    Task<AuthenticatedUserDto?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}

public interface IAccessControlService
{
    Task<bool> HasPermissionAsync(int userId, AppPermission permission, CancellationToken cancellationToken = default);
    Task<AuthenticatedUserDto> GetCurrentAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto> SaveUserAsync(UserSaveInput input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleDto> SaveRoleAsync(int? id, string name, string code, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(CancellationToken cancellationToken = default);
    Task SaveRolePermissionsAsync(int roleId, IReadOnlyList<AppPermission> permissions, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeManagerDto>> GetEmployeeManagersAsync(CancellationToken cancellationToken = default);
    Task SaveEmployeeManagersAsync(int employeeId, int? firstManagerUserId, int? secondManagerUserId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagerDashboardDto>> GetManagerDashboardAsync(int? periodId, CancellationToken cancellationToken = default);
}

public sealed record PageDefinitionDto(int Id, string Title, string Slug, string? Description, string LayoutJson,
    AppPermission RequiredPermission, bool IsActive);

public interface IPageDefinitionService
{
    Task<IReadOnlyList<PageDefinitionDto>> GetAccessibleAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PageDefinitionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PageDefinitionDto> SaveAsync(int? id, string title, string slug, string? description, string layoutJson,
        AppPermission requiredPermission, bool isActive, CancellationToken cancellationToken = default);
}

public sealed class AuthorizationException(string message = "Bu işlem için yetkiniz bulunmuyor.") : Exception(message);
