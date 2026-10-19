using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Workflow.Queries.GetEvaluationTasks;

public sealed class GetEvaluationTasksQueryHandler(IEvaluationWorkflowRepository repository, ICurrentUser currentUser) : IRequestHandler<GetEvaluationTasksQuery, IReadOnlyList<EvaluationTaskDto>>
{
    public Task<IReadOnlyList<EvaluationTaskDto>> Handle(GetEvaluationTasksQuery request, CancellationToken cancellationToken) =>
        repository.GetTasksAsync(request.PeriodId, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}

