using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Dashboard.Queries.GetManagerDashboard;

public sealed record GetManagerDashboardQuery(int? PeriodId) : IRequest<IReadOnlyList<ManagerDashboardDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.DashboardView;
}
