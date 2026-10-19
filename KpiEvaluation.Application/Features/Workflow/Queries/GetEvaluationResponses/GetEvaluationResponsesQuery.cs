using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Workflow.Queries.GetEvaluationResponses;

public sealed record GetEvaluationResponsesQuery(long EvaluationId) : IRequest<IReadOnlyList<EmployeeResponseDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationHistory;
}

