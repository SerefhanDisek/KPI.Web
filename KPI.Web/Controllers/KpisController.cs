using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Kpis.Commands.SaveKpi;
using KpiEvaluation.Application.Features.Kpis.Queries.GetKpis;
using KpiEvaluation.Application.Features.Kpis.Queries.GetKpisForProfession;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/kpis")]
public sealed class KpisController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<KpiDto>> Get(CancellationToken cancellationToken) =>
        sender.Send(new GetKpisQuery(), cancellationToken);

    [HttpGet("profession/{professionId:int}")]
    public Task<IReadOnlyList<KpiDto>> GetForProfession(int professionId, [FromQuery] int? periodId,
        CancellationToken cancellationToken) =>
        sender.Send(new GetKpisForProfessionQuery(professionId, periodId), cancellationToken);

    [HttpPost]
    public Task<KpiDto> Save(SaveKpiCommand command, CancellationToken cancellationToken) =>
        sender.Send(command, cancellationToken);
}
