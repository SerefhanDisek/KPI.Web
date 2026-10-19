using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Workflow.Queries.GetEvaluationTasks;

public sealed record GetEvaluationTasksQuery(int? PeriodId) : IRequest<IReadOnlyList<EvaluationTaskDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationTasks;
}

