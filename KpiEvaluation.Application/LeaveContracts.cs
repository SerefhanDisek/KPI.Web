namespace KpiEvaluation.Application;

public sealed record EmployeeLeaveDto(long Id, int EmployeeId, string EmployeeName, int ProfessionId, string ProfessionName,
    DateOnly StartDate, DateOnly EndDate, string LeaveType, string? Note, bool IsActive);
public sealed record EmployeeLeavePageDto(IReadOnlyList<EmployeeLeaveDto> Items, int TotalCount, int Page, int PageSize);
public sealed record EmployeeLeaveSaveInput(long? Id, int EmployeeId, DateOnly StartDate, DateOnly EndDate,
    string LeaveType, string? Note, bool IsActive);

public interface IEmployeeLeaveService
{
    Task<EmployeeLeavePageDto> GetAsync(int? employeeId, int? professionId, DateOnly? from, DateOnly? to,
        bool activeOnly, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<EmployeeLeaveDto> SaveAsync(EmployeeLeaveSaveInput input, int userId, CancellationToken cancellationToken = default);
}
