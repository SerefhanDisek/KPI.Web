using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface IAccessControlRepository
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
