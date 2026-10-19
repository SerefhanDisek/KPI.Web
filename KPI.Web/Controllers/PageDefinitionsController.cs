using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Pages.Commands.SavePageDefinition;
using KpiEvaluation.Application.Features.Pages.Queries.GetAccessiblePages;
using KpiEvaluation.Application.Features.Pages.Queries.GetPageDefinitions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/pages")]
public sealed class PageDefinitionsController(ISender sender) : ControllerBase
{
    [HttpGet("accessible")]
    public Task<IReadOnlyList<PageDefinitionDto>> GetAccessible(CancellationToken cancellationToken) => sender.Send(new GetAccessiblePagesQuery(), cancellationToken);

    [HttpGet]
    public Task<IReadOnlyList<PageDefinitionDto>> GetAll(CancellationToken cancellationToken) => sender.Send(new GetPageDefinitionsQuery(), cancellationToken);

    [HttpPost]
    public Task<PageDefinitionDto> Save(SavePageDefinitionCommand command, CancellationToken cancellationToken) => sender.Send(command, cancellationToken);
}
