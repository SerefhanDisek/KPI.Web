using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Queries.GetRoles;

public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
