using KpiEvaluation.Domain;

namespace KpiEvaluation.Application;

public sealed record EmployeeResponseDto(long Id, EmployeeResponseDecision Decision, string Comment, string UserName, DateTime CreatedAt);
public sealed record EvaluationTaskDto(int EmployeeId, string EmployeeName, int PeriodId, string PeriodName, DateOnly PeriodEndDate,
    int EvaluatorUserId, string EvaluatorName, long? EvaluationId, EvaluationStatus? Status);
public sealed record CalibrationScoreDto(long EvaluationId, int? EvaluatorUserId, string EvaluatorName, decimal Percentage);
public sealed record CalibrationRowDto(int EmployeeId, string EmployeeName, int PeriodId, string PeriodName,
    string ProfessionName, decimal Difference, IReadOnlyList<CalibrationScoreDto> Scores);
public sealed record AuditEntryDto(long Id, int? UserId, string? UserName, string EntityType, string EntityId, string Action,
    string ChangedFieldsJson, DateTime CreatedAt);
public sealed record AuditPageDto(IReadOnlyList<AuditEntryDto> Items, int TotalCount, int Page, int PageSize);
