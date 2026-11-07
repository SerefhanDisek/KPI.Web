using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Queries.GetTeamManagers;

public sealed class GetTeamManagersQueryHandler(IAccessControlRepository repository) : IRequestHandler<GetTeamManagersQuery, IReadOnlyList<TeamManagerDto>>
{
    public Task<IReadOnlyList<TeamManagerDto>> Handle(GetTeamManagersQuery request, CancellationToken cancellationToken) => repository.GetTeamManagersAsync(cancellationToken);
}
