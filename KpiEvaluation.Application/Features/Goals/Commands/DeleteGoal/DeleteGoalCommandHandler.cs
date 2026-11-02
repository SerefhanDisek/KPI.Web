using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Goals.Commands.DeleteGoal;

public sealed class DeleteGoalCommandHandler(ICompanyGoalRepository repository) : IRequestHandler<DeleteGoalCommand>
{
    public Task Handle(DeleteGoalCommand request, CancellationToken cancellationToken) =>
        repository.DeleteAsync(request.Id, cancellationToken);
}
