using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(int Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
