using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Employees.Commands.DeactivateEmployee;
using KpiEvaluation.Application.Features.Employees.Commands.DeleteEmployee;
using KpiEvaluation.Application.Features.Employees.Commands.SaveEmployee;
using KpiEvaluation.Application.Features.Employees.Queries.GetEmployees;
using KpiEvaluation.Application.Features.Professions.Queries.GetProfessionLookups;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<EmployeeDto>> Get([FromQuery] bool activeOnly, CancellationToken cancellationToken) =>
        sender.Send(new GetEmployeesQuery(activeOnly), cancellationToken);

    [HttpGet("profession-lookups")]
    public Task<IReadOnlyList<LookupDto>> GetProfessionLookups(CancellationToken cancellationToken) =>
        sender.Send(new GetProfessionLookupsQuery(), cancellationToken);

    [HttpPost]
    public Task<EmployeeDto> Save(SaveEmployeeCommand command, CancellationToken cancellationToken) =>
        sender.Send(command, cancellationToken);

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeactivateEmployeeCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteEmployeeCommand(id), cancellationToken);
        return NoContent();
    }
}
