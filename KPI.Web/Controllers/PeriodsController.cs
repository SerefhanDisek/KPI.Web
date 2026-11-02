using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Periods.Commands.DeactivatePeriod;
using KpiEvaluation.Application.Features.Periods.Commands.DeletePeriod;
using KpiEvaluation.Application.Features.Periods.Commands.SavePeriod;
using KpiEvaluation.Application.Features.Periods.Queries.GetPeriods;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/periods")]
public sealed class PeriodsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PeriodDto>> Get([FromQuery] bool activeOnly, CancellationToken cancellationToken) =>
        sender.Send(new GetPeriodsQuery(activeOnly), cancellationToken);

    [HttpPost]
    public Task<PeriodDto> Save(SavePeriodCommand command, CancellationToken cancellationToken) =>
        sender.Send(command, cancellationToken);

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeactivatePeriodCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePeriodCommand(id), cancellationToken);
        return NoContent();
    }
}
