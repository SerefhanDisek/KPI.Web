using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features;

public sealed record LoginCommand(string UserName, string Password) : IRequest<AuthenticatedUserDto?>;
public sealed record GetCurrentUserQuery : IRequest<AuthenticatedUserDto>;
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;
public sealed record GetUsersQuery : IRequest<IReadOnlyList<UserDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
public sealed record SaveUserCommand(UserSaveInput Input) : IRequest<UserDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
public sealed record SaveRoleCommand(int? Id, string Name, string Code, bool IsActive) : IRequest<RoleDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
public sealed record GetRolePermissionsQuery : IRequest<IReadOnlyList<RolePermissionDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
public sealed record SaveRolePermissionsCommand(int RoleId, IReadOnlyList<AppPermission> Permissions) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
public sealed record GetEmployeeManagersQuery : IRequest<IReadOnlyList<EmployeeManagerDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
public sealed record SaveEmployeeManagersCommand(int EmployeeId, int? FirstManagerUserId, int? SecondManagerUserId) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
public sealed record GetManagerDashboardQuery(int? PeriodId) : IRequest<IReadOnlyList<ManagerDashboardDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.DashboardView;
}
public sealed record GetAccessiblePagesQuery : IRequest<IReadOnlyList<PageDefinitionDto>>;
public sealed record GetPageDefinitionsQuery : IRequest<IReadOnlyList<PageDefinitionDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.PageDesignerManage;
}
public sealed record SavePageDefinitionCommand(int? Id, string Title, string Slug, string? Description, string LayoutJson,
    AppPermission RequiredPermission, bool IsActive) : IRequest<PageDefinitionDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.PageDesignerManage;
}
