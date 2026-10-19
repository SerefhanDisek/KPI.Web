using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.EmployeeLeaves.Commands.SaveEmployeeLeave;
using KpiEvaluation.Application.Features.EmployeeLeaves.Queries.GetEmployeeLeaves;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/leaves")]
public sealed class EmployeeLeavesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<EmployeeLeavePageDto> Get([FromQuery] int? employeeId, [FromQuery] int? professionId,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken,
        [FromQuery] bool activeOnly = true, [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        sender.Send(new GetEmployeeLeavesQuery(employeeId, professionId, from, to, activeOnly, page, pageSize), cancellationToken);

    [HttpPost]
    public Task<EmployeeLeaveDto> Save(EmployeeLeaveSaveInput input, CancellationToken cancellationToken) =>
        sender.Send(new SaveEmployeeLeaveCommand(input), cancellationToken);
}
