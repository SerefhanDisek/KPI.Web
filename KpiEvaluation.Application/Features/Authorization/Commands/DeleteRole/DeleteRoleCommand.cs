using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.DeleteRole;

public sealed record DeleteRoleCommand(int Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
