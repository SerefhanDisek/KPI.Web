using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Queries.GetTeamManagers;

public sealed record GetTeamManagersQuery : IRequest<IReadOnlyList<TeamManagerDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
