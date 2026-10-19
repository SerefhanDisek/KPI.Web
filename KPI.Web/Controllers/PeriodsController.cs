using KpiEvaluation.Application;
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
}
