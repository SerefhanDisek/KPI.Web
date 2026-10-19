using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Goals.Commands.SaveGoal;

public sealed record SaveGoalCommand(int? Id, string Name, string? Description, int? EvaluationPeriodId, int Priority, bool IsActive)
    : IRequest<CompanyGoalDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.GoalManage;
}
