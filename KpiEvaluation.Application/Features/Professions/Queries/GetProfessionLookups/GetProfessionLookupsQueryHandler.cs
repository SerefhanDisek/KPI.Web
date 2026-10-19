using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Professions.Queries.GetProfessionLookups;

public sealed class GetProfessionLookupsQueryHandler(IEmployeeRepository repository) : IRequestHandler<GetProfessionLookupsQuery, IReadOnlyList<LookupDto>>
{
    public Task<IReadOnlyList<LookupDto>> Handle(GetProfessionLookupsQuery request, CancellationToken cancellationToken) =>
        repository.GetProfessionsAsync(cancellationToken);
}
