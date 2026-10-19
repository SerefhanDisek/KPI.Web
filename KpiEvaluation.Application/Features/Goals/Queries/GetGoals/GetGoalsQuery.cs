using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Goals.Queries.GetGoals;

public sealed record GetGoalsQuery : IRequest<IReadOnlyList<CompanyGoalDto>>, IAnyAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.GoalManage, AppPermission.KpiManage, AppPermission.DashboardView];
}
