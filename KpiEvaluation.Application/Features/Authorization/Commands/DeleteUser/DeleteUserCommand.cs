using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.DeleteUser;

public sealed record DeleteUserCommand(int Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
