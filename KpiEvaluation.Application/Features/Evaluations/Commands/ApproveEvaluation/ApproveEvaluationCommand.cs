using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Commands.ApproveEvaluation;

public sealed record ApproveEvaluationCommand(long Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationApprove;
}
