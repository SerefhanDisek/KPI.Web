using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Workflow.Commands.RespondToEvaluation;
using KpiEvaluation.Application.Features.Workflow.Queries.GetCalibration;
using KpiEvaluation.Application.Features.Workflow.Queries.GetEvaluationResponses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/workflow")]
public sealed class WorkflowController(ISender sender) : ControllerBase
{
    [HttpGet("evaluations/{id:long}/responses")]
    public Task<IReadOnlyList<EmployeeResponseDto>> Responses(long id, CancellationToken cancellationToken) =>
        sender.Send(new GetEvaluationResponsesQuery(id), cancellationToken);

    [HttpPost("evaluations/{id:long}/responses")]
    public async Task<IActionResult> Respond(long id, RespondToEvaluationCommand command, CancellationToken cancellationToken)
    {
        if (id != command.EvaluationId) return BadRequest();
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("calibration")]
    public Task<IReadOnlyList<CalibrationRowDto>> Calibration([FromQuery] int? periodId, CancellationToken cancellationToken) =>
        sender.Send(new GetCalibrationQuery(periodId), cancellationToken);

}
