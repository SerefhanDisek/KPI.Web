using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Kpis.Commands.DeactivateKpi;

public sealed class DeactivateKpiCommandHandler(IKpiRepository repository) : IRequestHandler<DeactivateKpiCommand>
{
    public Task Handle(DeactivateKpiCommand request, CancellationToken cancellationToken) =>
        repository.DeactivateAsync(request.Id, cancellationToken);
}
