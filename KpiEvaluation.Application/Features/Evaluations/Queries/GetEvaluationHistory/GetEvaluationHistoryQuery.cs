using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Queries.GetEvaluationHistory;

public sealed record GetEvaluationHistoryQuery(int? EmployeeId, int? ProfessionId, int? PeriodId, int? EvaluatorUserId = null)
    : IRequest<IReadOnlyList<EvaluationSummaryDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationHistory;
}
