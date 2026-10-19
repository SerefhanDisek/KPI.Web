using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Workflow.Queries.GetCalibration;

public sealed record GetCalibrationQuery(int? PeriodId) : IRequest<IReadOnlyList<CalibrationRowDto>>, IAllAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.CalibrationView, AppPermission.EvaluationHistory];
}

