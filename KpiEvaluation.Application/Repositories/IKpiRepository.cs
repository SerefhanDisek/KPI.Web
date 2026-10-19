using KpiEvaluation.Domain;

namespace KpiEvaluation.Application.Repositories;

public interface IKpiRepository
{
    Task<IReadOnlyList<KpiDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KpiDto>> GetForProfessionAsync(int professionId, int? evaluationPeriodId = null, CancellationToken cancellationToken = default);
    Task<KpiDto> SaveAsync(int? id, string title, string? description, int? professionId, bool isCommon, int displayOrder, bool isActive, CancellationToken cancellationToken = default,
        string? category = null, int? companyGoalId = null, KpiMeasurementType measurementType = KpiMeasurementType.ManualScore,
        string? unit = null, decimal? targetValue = null, KpiTargetDirection targetDirection = KpiTargetDirection.AtLeast,
        decimal weightPercent = 0, string? dataSource = null, KpiReviewFrequency reviewFrequency = KpiReviewFrequency.SemiAnnual,
        DateOnly? validFrom = null, DateOnly? validTo = null, bool isSpecific = false, bool isMeasurable = false,
        bool isAchievable = false, bool isRelevant = false, bool isTimeBound = false, IReadOnlyList<int>? professionIds = null);
}
