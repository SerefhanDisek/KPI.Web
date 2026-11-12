using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Commands.ApproveEvaluation;

public sealed record ApproveEvaluationCommand(long Id) : IRequest, IAllAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.EvaluationHistory, AppPermission.EvaluationCreate, AppPermission.EvaluationApprove];
}
