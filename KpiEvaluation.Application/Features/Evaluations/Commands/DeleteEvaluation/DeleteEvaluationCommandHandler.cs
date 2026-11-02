using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Commands.DeleteEvaluation;

public sealed class DeleteEvaluationCommandHandler(IEvaluationRepository repository, ICurrentUser currentUser) : IRequestHandler<DeleteEvaluationCommand>
{
    public Task Handle(DeleteEvaluationCommand request, CancellationToken cancellationToken) =>
        repository.DeleteAsync(request.Id, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}
