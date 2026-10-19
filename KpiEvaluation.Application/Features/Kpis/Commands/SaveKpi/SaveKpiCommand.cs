using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Kpis.Commands.SaveKpi;

public sealed record SaveKpiCommand(
    int? Id,
    string Title,
    string? Description,
    int? ProfessionId,
    bool IsCommon,
    int DisplayOrder,
    bool IsActive,
    string? Category,
    int? CompanyGoalId,
    KpiMeasurementType MeasurementType,
    string? Unit,
    decimal? TargetValue,
    KpiTargetDirection TargetDirection,
    decimal WeightPercent,
    string? DataSource,
    KpiReviewFrequency ReviewFrequency,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool IsSpecific,
    bool IsMeasurable,
    bool IsAchievable,
    bool IsRelevant,
    bool IsTimeBound,
    IReadOnlyList<int>? ProfessionIds = null) : IRequest<KpiDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.KpiManage;
}
