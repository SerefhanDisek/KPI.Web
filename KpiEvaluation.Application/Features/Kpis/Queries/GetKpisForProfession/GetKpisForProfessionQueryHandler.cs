using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Kpis.Queries.GetKpisForProfession;

public sealed class GetKpisForProfessionQueryHandler(IKpiRepository repository) : IRequestHandler<GetKpisForProfessionQuery, IReadOnlyList<KpiDto>>
{
    public Task<IReadOnlyList<KpiDto>> Handle(GetKpisForProfessionQuery request, CancellationToken cancellationToken) =>
        repository.GetForProfessionAsync(request.ProfessionId, request.EvaluationPeriodId, cancellationToken);
}
