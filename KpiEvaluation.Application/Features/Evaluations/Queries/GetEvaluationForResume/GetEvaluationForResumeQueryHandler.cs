using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Queries.GetEvaluationForResume;

public sealed class GetEvaluationForResumeQueryHandler(IEvaluationRepository repository, ICurrentUser currentUser) : IRequestHandler<GetEvaluationForResumeQuery, EvaluationResumeDto?>
{
    public Task<EvaluationResumeDto?> Handle(GetEvaluationForResumeQuery request, CancellationToken cancellationToken) =>
        repository.GetForResumeAsync(request.EmployeeId, request.PeriodId, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}
