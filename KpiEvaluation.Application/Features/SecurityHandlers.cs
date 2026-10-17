using MediatR;

namespace KpiEvaluation.Application.Features;

public sealed class LoginHandler(IAuthenticationService service) : IRequestHandler<LoginCommand, AuthenticatedUserDto?>
{
    public Task<AuthenticatedUserDto?> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        service.AuthenticateAsync(request.UserName, request.Password, cancellationToken);
}

public sealed class GetCurrentUserHandler(ICurrentUser currentUser, IAccessControlService service)
    : IRequestHandler<GetCurrentUserQuery, AuthenticatedUserDto>
{
    public Task<AuthenticatedUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken) =>
        service.GetCurrentAsync(currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}

public sealed class ChangePasswordHandler(ICurrentUser currentUser, IAuthenticationService service) : IRequestHandler<ChangePasswordCommand>
{
    public Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken) =>
        service.ChangePasswordAsync(currentUser.UserId ?? throw new AuthorizationException(), request.CurrentPassword,
            request.NewPassword, cancellationToken);
}

public sealed class GetUsersHandler(IAccessControlService service) : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken) => service.GetUsersAsync(cancellationToken);
}
public sealed class SaveUserHandler(IAccessControlService service) : IRequestHandler<SaveUserCommand, UserDto>
{
    public Task<UserDto> Handle(SaveUserCommand request, CancellationToken cancellationToken) => service.SaveUserAsync(request.Input, cancellationToken);
}
public sealed class GetRolesHandler(IAccessControlService service) : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken) => service.GetRolesAsync(cancellationToken);
}
public sealed class SaveRoleHandler(IAccessControlService service) : IRequestHandler<SaveRoleCommand, RoleDto>
{
    public Task<RoleDto> Handle(SaveRoleCommand request, CancellationToken cancellationToken) => service.SaveRoleAsync(request.Id, request.Name, request.Code, request.IsActive, cancellationToken);
}
public sealed class GetRolePermissionsHandler(IAccessControlService service) : IRequestHandler<GetRolePermissionsQuery, IReadOnlyList<RolePermissionDto>>
{
    public Task<IReadOnlyList<RolePermissionDto>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken) => service.GetRolePermissionsAsync(cancellationToken);
}
public sealed class SaveRolePermissionsHandler(IAccessControlService service) : IRequestHandler<SaveRolePermissionsCommand>
{
    public Task Handle(SaveRolePermissionsCommand request, CancellationToken cancellationToken) => service.SaveRolePermissionsAsync(request.RoleId, request.Permissions, cancellationToken);
}
public sealed class GetEmployeeManagersHandler(IAccessControlService service) : IRequestHandler<GetEmployeeManagersQuery, IReadOnlyList<EmployeeManagerDto>>
{
    public Task<IReadOnlyList<EmployeeManagerDto>> Handle(GetEmployeeManagersQuery request, CancellationToken cancellationToken) => service.GetEmployeeManagersAsync(cancellationToken);
}
public sealed class SaveEmployeeManagersHandler(IAccessControlService service) : IRequestHandler<SaveEmployeeManagersCommand>
{
    public Task Handle(SaveEmployeeManagersCommand request, CancellationToken cancellationToken) => service.SaveEmployeeManagersAsync(request.EmployeeId, request.FirstManagerUserId, request.SecondManagerUserId, cancellationToken);
}
public sealed class GetManagerDashboardHandler(IAccessControlService service) : IRequestHandler<GetManagerDashboardQuery, IReadOnlyList<ManagerDashboardDto>>
{
    public Task<IReadOnlyList<ManagerDashboardDto>> Handle(GetManagerDashboardQuery request, CancellationToken cancellationToken) => service.GetManagerDashboardAsync(request.PeriodId, cancellationToken);
}
public sealed class GetAccessiblePagesHandler(ICurrentUser currentUser, IPageDefinitionService service) : IRequestHandler<GetAccessiblePagesQuery, IReadOnlyList<PageDefinitionDto>>
{
    public Task<IReadOnlyList<PageDefinitionDto>> Handle(GetAccessiblePagesQuery request, CancellationToken cancellationToken) => service.GetAccessibleAsync(currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}
public sealed class GetPageDefinitionsHandler(IPageDefinitionService service) : IRequestHandler<GetPageDefinitionsQuery, IReadOnlyList<PageDefinitionDto>>
{
    public Task<IReadOnlyList<PageDefinitionDto>> Handle(GetPageDefinitionsQuery request, CancellationToken cancellationToken) => service.GetAllAsync(cancellationToken);
}
public sealed class SavePageDefinitionHandler(IPageDefinitionService service) : IRequestHandler<SavePageDefinitionCommand, PageDefinitionDto>
{
    public Task<PageDefinitionDto> Handle(SavePageDefinitionCommand request, CancellationToken cancellationToken) => service.SaveAsync(request.Id, request.Title, request.Slug, request.Description, request.LayoutJson, request.RequiredPermission, request.IsActive, cancellationToken);
}
