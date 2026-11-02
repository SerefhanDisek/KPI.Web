using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Kpis.Commands.DeleteKpi;

public sealed class DeleteKpiCommandHandler(IKpiRepository repository) : IRequestHandler<DeleteKpiCommand>
{
    public Task Handle(DeleteKpiCommand request, CancellationToken cancellationToken) =>
        repository.DeleteAsync(request.Id, cancellationToken);
}
