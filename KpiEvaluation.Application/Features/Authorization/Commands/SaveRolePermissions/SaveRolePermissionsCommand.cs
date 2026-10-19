using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.SaveRolePermissions;

public sealed record SaveRolePermissionsCommand(int RoleId, IReadOnlyList<AppPermission> Permissions) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
