using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface IEmployeeLeaveRepository
{
    Task<EmployeeLeavePageDto> GetAsync(int? employeeId, int? professionId, DateOnly? from, DateOnly? to,
        bool activeOnly, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<EmployeeLeaveDto> SaveAsync(EmployeeLeaveSaveInput input, int userId, CancellationToken cancellationToken = default);
}
