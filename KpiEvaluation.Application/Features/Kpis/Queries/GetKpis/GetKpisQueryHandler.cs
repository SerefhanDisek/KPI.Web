using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Kpis.Queries.GetKpis;

public sealed class GetKpisQueryHandler(IKpiRepository repository) : IRequestHandler<GetKpisQuery, IReadOnlyList<KpiDto>>
{
    public Task<IReadOnlyList<KpiDto>> Handle(GetKpisQuery request, CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);
}
