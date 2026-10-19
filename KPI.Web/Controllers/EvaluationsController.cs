using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Evaluations.Commands.ApproveEvaluation;
using KpiEvaluation.Application.Features.Evaluations.Commands.SaveEvaluation;
using KpiEvaluation.Application.Features.Evaluations.Commands.UpdateEvaluation;
using KpiEvaluation.Application.Features.Evaluations.Queries.GetEvaluationDetails;
using KpiEvaluation.Application.Features.Evaluations.Queries.GetEvaluationForEdit;
using KpiEvaluation.Application.Features.Evaluations.Queries.GetEvaluationHistory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/evaluations")]
public sealed class EvaluationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<EvaluationSummaryDto>> GetHistory([FromQuery] int? employeeId,
        [FromQuery] int? professionId, [FromQuery] int? periodId, [FromQuery] int? evaluatorUserId, CancellationToken cancellationToken) =>
        sender.Send(new GetEvaluationHistoryQuery(employeeId, professionId, periodId, evaluatorUserId), cancellationToken);

    [HttpGet("{id:long}/details")]
    public Task<IReadOnlyList<EvaluationDetailDto>> GetDetails(long id, CancellationToken cancellationToken) =>
        sender.Send(new GetEvaluationDetailsQuery(id), cancellationToken);

    [HttpGet("{id:long}/edit")]
    public Task<EvaluationEditDto> GetForEdit(long id, CancellationToken cancellationToken) =>
        sender.Send(new GetEvaluationForEditQuery(id), cancellationToken);

    [HttpPost]
    public async Task<IActionResult> Save(EvaluationSaveInput input, CancellationToken cancellationToken)
    {
        await sender.Send(new SaveEvaluationCommand(input), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateEvaluationCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest();
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/approve")]
    public async Task<IActionResult> Approve(long id, CancellationToken cancellationToken)
    {
        await sender.Send(new ApproveEvaluationCommand(id), cancellationToken);
        return NoContent();
    }
}
