using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Queries.GetEvaluationForEdit;

public sealed record GetEvaluationForEditQuery(long Id) : IRequest<EvaluationEditDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationCreate;
}
