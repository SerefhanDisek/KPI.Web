using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Commands.SaveEvaluation;

public sealed class SaveEvaluationCommandHandler(IEvaluationRepository repository, ICurrentUser currentUser) : IRequestHandler<SaveEvaluationCommand>
{
    public Task Handle(SaveEvaluationCommand request, CancellationToken cancellationToken) =>
        repository.SaveAsync(request.Input, cancellationToken, currentUser.UserId);
}
