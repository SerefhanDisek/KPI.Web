using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface IEvaluationImportRepository
{
    Task<ImportResultDto> ImportAsync(IReadOnlyList<PortableEvaluationDto> evaluations, CancellationToken cancellationToken = default);
}
