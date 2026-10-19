using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface IDashboardRepository
{
    Task<DashboardDto> GetAsync(int? periodId, CancellationToken cancellationToken = default, int? requesterUserId = null, int? evaluatorUserId = null);
    Task<IReadOnlyList<PortableEvaluationDto>> GetPortableEvaluationsAsync(int? periodId, CancellationToken cancellationToken = default);
}
