using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Periods.Commands.DeletePeriod;

public sealed class DeletePeriodCommandHandler(IEvaluationPeriodRepository repository) : IRequestHandler<DeletePeriodCommand>
{
    public Task Handle(DeletePeriodCommand request, CancellationToken cancellationToken) =>
        repository.DeleteAsync(request.Id, cancellationToken);
}
