using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Queries.GetEvaluationForEdit;

public sealed class GetEvaluationForEditQueryHandler(IEvaluationRepository repository, ICurrentUser currentUser) : IRequestHandler<GetEvaluationForEditQuery, EvaluationEditDto>
{
    public Task<EvaluationEditDto> Handle(GetEvaluationForEditQuery request, CancellationToken cancellationToken) =>
        repository.GetForEditAsync(request.Id, cancellationToken, currentUser.UserId);
}
