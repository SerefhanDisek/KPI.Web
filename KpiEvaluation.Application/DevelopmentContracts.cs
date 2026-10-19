using KpiEvaluation.Domain;

namespace KpiEvaluation.Application;

public sealed record CompetencyMatrixEntryDto(int Id, int ProfessionId, string ProfessionName, string Seniority, string Competency, string ExpectedLevel, bool IsActive, int Version);
public sealed record CompetencyMatrixDto(IReadOnlyList<CompetencyMatrixEntryDto> Items, IReadOnlyList<LookupDto> Professions);
public sealed record CompetencyMatrixSaveInput(int? Id, int ProfessionId, string Seniority, string Competency, string ExpectedLevel, bool IsActive, int? ExpectedVersion = null);
public sealed record DevelopmentItemDto(long Id, int EmployeeId, string EmployeeName, DevelopmentItemKind Kind, string Title,
    string? Description, DateOnly? TargetDate, decimal Progress, bool IsActive, int Version, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record DevelopmentBoardDto(IReadOnlyList<DevelopmentItemDto> Items, IReadOnlyList<LookupDto> Employees, int TotalCount, int Page, int PageSize);
public sealed record DevelopmentItemSaveInput(long? Id, int EmployeeId, DevelopmentItemKind Kind, string Title,
    string? Description, DateOnly? TargetDate, decimal Progress, bool IsActive, int? ExpectedVersion = null);
public sealed record DevelopmentCheckInDto(long Id, string Note, decimal Progress, string CreatedByName, DateTime CreatedAt);
