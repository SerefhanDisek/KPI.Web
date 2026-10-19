using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Queries.GetEvaluationDetails;

public sealed class GetEvaluationDetailsQueryHandler(IEvaluationRepository repository, ICurrentUser currentUser)
    : IRequestHandler<GetEvaluationDetailsQuery, IReadOnlyList<EvaluationDetailDto>>
{
    public Task<IReadOnlyList<EvaluationDetailDto>> Handle(GetEvaluationDetailsQuery request, CancellationToken cancellationToken) =>
        repository.GetDetailsAsync(request.Id, cancellationToken, currentUser.UserId);
}
