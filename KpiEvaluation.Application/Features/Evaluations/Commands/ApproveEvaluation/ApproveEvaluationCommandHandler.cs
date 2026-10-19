using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Commands.ApproveEvaluation;

public sealed class ApproveEvaluationCommandHandler(IEvaluationRepository repository, ICurrentUser currentUser) : IRequestHandler<ApproveEvaluationCommand>
{
    public Task Handle(ApproveEvaluationCommand request, CancellationToken cancellationToken) =>
        repository.ApproveAsync(request.Id, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}
