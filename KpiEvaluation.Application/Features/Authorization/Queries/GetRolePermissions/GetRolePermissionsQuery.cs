using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Queries.GetRolePermissions;

public sealed record GetRolePermissionsQuery : IRequest<IReadOnlyList<RolePermissionDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
