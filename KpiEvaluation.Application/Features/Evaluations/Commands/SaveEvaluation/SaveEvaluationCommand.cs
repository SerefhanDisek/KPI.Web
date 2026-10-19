using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Commands.SaveEvaluation;

public sealed record SaveEvaluationCommand(EvaluationSaveInput Input) : IRequest, IAllAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => Input.Status == EvaluationStatus.Draft
        ? [AppPermission.EvaluationCreate] : [AppPermission.EvaluationCreate, AppPermission.EvaluationApprove];
}
