using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Queries.GetEvaluationHistory;

public sealed class GetEvaluationHistoryQueryHandler(IEvaluationRepository repository, ICurrentUser currentUser)
    : IRequestHandler<GetEvaluationHistoryQuery, IReadOnlyList<EvaluationSummaryDto>>
{
    public Task<IReadOnlyList<EvaluationSummaryDto>> Handle(GetEvaluationHistoryQuery request, CancellationToken cancellationToken) =>
        repository.GetHistoryAsync(request.EmployeeId, request.ProfessionId, request.PeriodId, cancellationToken, currentUser.UserId, request.EvaluatorUserId);
}
