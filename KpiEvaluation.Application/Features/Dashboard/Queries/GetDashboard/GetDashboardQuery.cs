using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Dashboard.Queries.GetDashboard;

public sealed record GetDashboardQuery(int? PeriodId, int? EvaluatorUserId = null) : IRequest<DashboardDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.DashboardView;
}
