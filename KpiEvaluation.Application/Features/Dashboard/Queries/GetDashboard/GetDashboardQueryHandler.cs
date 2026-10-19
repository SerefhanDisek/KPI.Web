using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Dashboard.Queries.GetDashboard;

public sealed class GetDashboardQueryHandler(IDashboardRepository repository, ICurrentUser currentUser) : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken) =>
        repository.GetAsync(request.PeriodId, cancellationToken, currentUser.UserId, request.EvaluatorUserId);
}
