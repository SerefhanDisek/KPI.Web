using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface IEvaluationWorkflowRepository
{
    Task<IReadOnlyList<EmployeeResponseDto>> GetResponsesAsync(long evaluationId, int userId, CancellationToken cancellationToken);
    Task RespondAsync(long evaluationId, EmployeeResponseDecision decision, string comment, int userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EvaluationTaskDto>> GetTasksAsync(int? periodId, int userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CalibrationRowDto>> GetCalibrationAsync(int? periodId, int userId, CancellationToken cancellationToken);
    Task<AuditPageDto> GetAuditAsync(int page, int pageSize, string? entityType, CancellationToken cancellationToken);
}
