using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Workflow.Queries.GetEvaluationResponses;

public sealed class GetEvaluationResponsesQueryHandler(IEvaluationWorkflowRepository repository, ICurrentUser currentUser) : IRequestHandler<GetEvaluationResponsesQuery, IReadOnlyList<EmployeeResponseDto>>
{
    public Task<IReadOnlyList<EmployeeResponseDto>> Handle(GetEvaluationResponsesQuery request, CancellationToken cancellationToken) =>
        repository.GetResponsesAsync(request.EvaluationId, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}

