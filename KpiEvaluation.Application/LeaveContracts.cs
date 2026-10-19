namespace KpiEvaluation.Application;

public sealed record EmployeeLeaveDto(long Id, int EmployeeId, string EmployeeName, int ProfessionId, string ProfessionName,
    DateOnly StartDate, DateOnly EndDate, string LeaveType, string? Note, bool IsActive);
public sealed record EmployeeLeavePageDto(IReadOnlyList<EmployeeLeaveDto> Items, int TotalCount, int Page, int PageSize);
public sealed record EmployeeLeaveSaveInput(long? Id, int EmployeeId, DateOnly StartDate, DateOnly EndDate,
    string LeaveType, string? Note, bool IsActive);
