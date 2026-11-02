using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Goals.Commands.DeleteGoal;

public sealed record DeleteGoalCommand(int Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.GoalManage;
}
