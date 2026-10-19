using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Development.Queries.GetDevelopmentCheckIns;

public sealed class GetDevelopmentCheckInsQueryHandler(IDevelopmentRepository repository, ICurrentUser currentUser) : IRequestHandler<GetDevelopmentCheckInsQuery, IReadOnlyList<DevelopmentCheckInDto>>
{
    public Task<IReadOnlyList<DevelopmentCheckInDto>> Handle(GetDevelopmentCheckInsQuery request, CancellationToken cancellationToken) =>
        repository.GetCheckInsAsync(request.Id, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}

