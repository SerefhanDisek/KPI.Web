using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Kpis.Commands.SaveKpi;

public sealed class SaveKpiCommandHandler(IKpiRepository repository) : IRequestHandler<SaveKpiCommand, KpiDto>
{
    public Task<KpiDto> Handle(SaveKpiCommand request, CancellationToken cancellationToken) =>
        repository.SaveAsync(request.Id, request.Title, request.Description, request.ProfessionId, request.IsCommon,
            request.DisplayOrder, request.IsActive, cancellationToken, request.Category, request.CompanyGoalId,
            request.MeasurementType, request.Unit, request.TargetValue, request.TargetDirection, request.WeightPercent,
            request.DataSource, request.ReviewFrequency, request.ValidFrom, request.ValidTo, request.IsSpecific,
            request.IsMeasurable, request.IsAchievable, request.IsRelevant, request.IsTimeBound, request.ProfessionIds);
}
