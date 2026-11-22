using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Workflow.Queries.GetCalibration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/workflow")]
public sealed class WorkflowController(ISender sender) : ControllerBase
{
    [HttpGet("calibration")]
    public Task<IReadOnlyList<CalibrationRowDto>> Calibration([FromQuery] int? periodId, CancellationToken cancellationToken) =>
        sender.Send(new GetCalibrationQuery(periodId), cancellationToken);

}
