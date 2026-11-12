using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.SaveTeamManagers;

public sealed record SaveTeamManagersCommand(int ProfessionId, IReadOnlyList<TeamManagerInput> Managers) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
