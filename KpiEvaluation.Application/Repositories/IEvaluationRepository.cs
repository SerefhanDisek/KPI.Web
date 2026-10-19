using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface IEvaluationRepository
{
    Task SaveAsync(EvaluationSaveInput input, CancellationToken cancellationToken = default, int? evaluatorUserId = null);
    Task<IReadOnlyList<EvaluationSummaryDto>> GetHistoryAsync(int? employeeId, int? professionId, int? periodId, CancellationToken cancellationToken = default,
        int? requesterUserId = null, int? evaluatorUserId = null);
    Task<IReadOnlyList<EvaluationDetailDto>> GetDetailsAsync(long evaluationId, CancellationToken cancellationToken = default, int? requesterUserId = null);
    Task<EvaluationEditDto> GetForEditAsync(long evaluationId, CancellationToken cancellationToken = default, int? requesterUserId = null);
    Task UpdateAsync(long evaluationId, string? generalNote, IReadOnlyList<EvaluationItemUpdateInput> items, CancellationToken cancellationToken = default,
        EvaluationStatus status = EvaluationStatus.Completed, string? managerFeedback = null, string? employeeFeedback = null,
        string? developmentAction = null, DateOnly? followUpDate = null, int? requesterUserId = null);
    Task ApproveAsync(long evaluationId, int approverUserId, CancellationToken cancellationToken = default);
}
