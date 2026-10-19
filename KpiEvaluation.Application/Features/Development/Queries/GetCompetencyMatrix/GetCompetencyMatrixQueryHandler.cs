using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Development.Queries.GetCompetencyMatrix;

public sealed class GetCompetencyMatrixQueryHandler(IDevelopmentRepository repository) : IRequestHandler<GetCompetencyMatrixQuery, CompetencyMatrixDto>
{
    public Task<CompetencyMatrixDto> Handle(GetCompetencyMatrixQuery request, CancellationToken cancellationToken) =>
        repository.GetMatrixAsync(cancellationToken);
}

