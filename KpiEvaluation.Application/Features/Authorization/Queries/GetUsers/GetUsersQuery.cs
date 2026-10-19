using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Queries.GetUsers;

public sealed record GetUsersQuery : IRequest<IReadOnlyList<UserDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
