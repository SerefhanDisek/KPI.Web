using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Professions.Queries.GetProfessions;

public sealed class GetProfessionsQueryHandler(IProfessionRepository repository) : IRequestHandler<GetProfessionsQuery, IReadOnlyList<ProfessionDto>>
{
    public Task<IReadOnlyList<ProfessionDto>> Handle(GetProfessionsQuery request, CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);
}
