using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface IProfessionRepository
{
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfessionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProfessionDto> SaveAsync(int? id, string name, bool isActive, CancellationToken cancellationToken = default);
}
