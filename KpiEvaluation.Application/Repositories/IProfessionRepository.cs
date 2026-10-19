using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface IProfessionRepository
{
    Task<IReadOnlyList<ProfessionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProfessionDto> SaveAsync(int? id, string name, bool isActive, CancellationToken cancellationToken = default);
}
