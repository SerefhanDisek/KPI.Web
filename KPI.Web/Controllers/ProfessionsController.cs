using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Professions.Commands.DeactivateProfession;
using KpiEvaluation.Application.Features.Professions.Commands.DeleteProfession;
using KpiEvaluation.Application.Features.Professions.Commands.SaveProfession;
using KpiEvaluation.Application.Features.Professions.Queries.GetProfessions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/professions")]
public sealed class ProfessionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ProfessionDto>> Get(CancellationToken cancellationToken) =>
        sender.Send(new GetProfessionsQuery(), cancellationToken);

    [HttpPost]
    public Task<ProfessionDto> Save(SaveProfessionCommand command, CancellationToken cancellationToken) =>
        sender.Send(command, cancellationToken);

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeactivateProfessionCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProfessionCommand(id), cancellationToken);
        return NoContent();
    }
}
