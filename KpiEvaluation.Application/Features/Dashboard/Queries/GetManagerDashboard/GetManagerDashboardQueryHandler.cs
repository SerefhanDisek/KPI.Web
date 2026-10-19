using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Dashboard.Queries.GetManagerDashboard;

public sealed class GetManagerDashboardQueryHandler(IAccessControlRepository repository) : IRequestHandler<GetManagerDashboardQuery, IReadOnlyList<ManagerDashboardDto>>
{
    public Task<IReadOnlyList<ManagerDashboardDto>> Handle(GetManagerDashboardQuery request, CancellationToken cancellationToken) => repository.GetManagerDashboardAsync(request.PeriodId, cancellationToken);
}
