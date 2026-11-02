using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Periods.Commands.DeactivatePeriod;

public sealed class DeactivatePeriodCommandHandler(IEvaluationPeriodRepository repository) : IRequestHandler<DeactivatePeriodCommand>
{
    public Task Handle(DeactivatePeriodCommand request, CancellationToken cancellationToken) =>
        repository.DeactivateAsync(request.Id, cancellationToken);
}
