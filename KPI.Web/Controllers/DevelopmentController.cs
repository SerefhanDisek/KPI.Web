using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Development.Commands.AddDevelopmentCheckIn;
using KpiEvaluation.Application.Features.Development.Commands.SaveDevelopmentItem;
using KpiEvaluation.Application.Features.Development.Queries.GetDevelopmentBoard;
using KpiEvaluation.Application.Features.Development.Queries.GetDevelopmentCheckIns;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/development")]
public sealed class DevelopmentController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<DevelopmentBoardDto> Board([FromQuery] int? employeeId, [FromQuery] bool activeOnly = true,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        sender.Send(new GetDevelopmentBoardQuery(employeeId, activeOnly, page, pageSize), cancellationToken);

    [HttpPost]
    public Task<DevelopmentItemDto> Save(DevelopmentItemSaveInput input, CancellationToken cancellationToken) =>
        sender.Send(new SaveDevelopmentItemCommand(input), cancellationToken);

    [HttpGet("{id:long}/check-ins")]
    public Task<IReadOnlyList<DevelopmentCheckInDto>> CheckIns(long id, CancellationToken cancellationToken) =>
        sender.Send(new GetDevelopmentCheckInsQuery(id), cancellationToken);

    [HttpPost("{id:long}/check-ins")]
    public async Task<IActionResult> AddCheckIn(long id, AddDevelopmentCheckInCommand command, CancellationToken cancellationToken)
    {
        if(id != command.Id) return BadRequest();
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
