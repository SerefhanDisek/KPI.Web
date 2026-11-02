using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Goals.Commands.DeactivateGoal;

public sealed record DeactivateGoalCommand(int Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.GoalManage;
}
