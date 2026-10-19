using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.SaveRole;

public sealed record SaveRoleCommand(int? Id, string Name, string Code, bool IsActive) : IRequest<RoleDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
