using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.SaveUser;

public sealed record SaveUserCommand(UserSaveInput Input) : IRequest<UserDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
