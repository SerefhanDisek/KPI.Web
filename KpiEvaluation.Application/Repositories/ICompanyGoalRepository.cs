using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface ICompanyGoalRepository
{
    Task<IReadOnlyList<CompanyGoalDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CompanyGoalDto> SaveAsync(int? id, string name, string? description, int? evaluationPeriodId, int priority, bool isActive, CancellationToken cancellationToken = default);
}
