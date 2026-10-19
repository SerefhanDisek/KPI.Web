using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Workflow.Queries.GetCalibration;

public sealed class GetCalibrationQueryHandler(IEvaluationWorkflowRepository repository, ICurrentUser currentUser) : IRequestHandler<GetCalibrationQuery, IReadOnlyList<CalibrationRowDto>>
{
    public Task<IReadOnlyList<CalibrationRowDto>> Handle(GetCalibrationQuery request, CancellationToken cancellationToken) =>
        repository.GetCalibrationAsync(request.PeriodId, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}

