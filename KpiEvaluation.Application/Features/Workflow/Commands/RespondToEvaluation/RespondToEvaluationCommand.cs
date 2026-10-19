using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Workflow.Commands.RespondToEvaluation;

public sealed record RespondToEvaluationCommand(long EvaluationId, EmployeeResponseDecision Decision, string Comment) : IRequest, IAllAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.EvaluationHistory, AppPermission.EvaluationRespond];
}

