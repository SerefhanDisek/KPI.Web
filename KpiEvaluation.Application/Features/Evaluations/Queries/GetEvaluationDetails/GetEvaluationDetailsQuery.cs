using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Queries.GetEvaluationDetails;

public sealed record GetEvaluationDetailsQuery(long Id) : IRequest<IReadOnlyList<EvaluationDetailDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationHistory;
}
