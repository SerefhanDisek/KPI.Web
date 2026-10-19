using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Periods.Queries.GetPeriods;

public sealed class GetPeriodsQueryHandler(IEvaluationPeriodRepository repository) : IRequestHandler<GetPeriodsQuery, IReadOnlyList<PeriodDto>>
{
    public Task<IReadOnlyList<PeriodDto>> Handle(GetPeriodsQuery request, CancellationToken cancellationToken) =>
        repository.GetAllAsync(request.ActiveOnly, cancellationToken);
}
