using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Periods.Queries.GetPeriods;

public sealed record GetPeriodsQuery(bool ActiveOnly) : IRequest<IReadOnlyList<PeriodDto>>, IAnyAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.PeriodManage, AppPermission.EvaluationCreate, AppPermission.EvaluationHistory, AppPermission.DashboardView, AppPermission.GoalManage];
}
