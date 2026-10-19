using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface IEvaluationPeriodRepository
{
    Task<IReadOnlyList<PeriodDto>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<PeriodDto> SaveAsync(int? id, string name, DateOnly startDate, DateOnly endDate, bool isActive, CancellationToken cancellationToken = default);
}
