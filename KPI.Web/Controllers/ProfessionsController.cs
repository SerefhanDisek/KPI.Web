using KpiEvaluation.Application;
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
}
