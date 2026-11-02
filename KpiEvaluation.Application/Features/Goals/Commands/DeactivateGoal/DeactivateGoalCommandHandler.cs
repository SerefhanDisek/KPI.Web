using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Goals.Commands.DeactivateGoal;

public sealed class DeactivateGoalCommandHandler(ICompanyGoalRepository repository) : IRequestHandler<DeactivateGoalCommand>
{
    public Task Handle(DeactivateGoalCommand request, CancellationToken cancellationToken) =>
        repository.DeactivateAsync(request.Id, cancellationToken);
}
