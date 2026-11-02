using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Commands.DeleteEvaluation;

public sealed record DeleteEvaluationCommand(long Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationHistory;
}
