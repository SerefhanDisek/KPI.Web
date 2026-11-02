using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface IEmployeeRepository
{
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupDto>> GetProfessionsAsync(CancellationToken cancellationToken = default);
    Task<EmployeeDto> SaveAsync(int? id, string fullName, int professionId, bool isActive, CancellationToken cancellationToken = default);
}
