using KpiEvaluation.Application;
using KpiEvaluation.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<DashboardDto> Get([FromQuery] int? periodId, [FromQuery] int? evaluatorUserId, CancellationToken cancellationToken) =>
        sender.Send(new GetDashboardQuery(periodId, evaluatorUserId), cancellationToken);

    [HttpGet("managers")]
    public Task<IReadOnlyList<ManagerDashboardDto>> GetManagers([FromQuery] int? periodId, CancellationToken cancellationToken) =>
        sender.Send(new GetManagerDashboardQuery(periodId), cancellationToken);
}

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
}

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

[ApiController]
[Route("api/goals")]
public sealed class GoalsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CompanyGoalDto>> Get(CancellationToken cancellationToken) =>
        sender.Send(new GetGoalsQuery(), cancellationToken);

    [HttpPost]
    public Task<CompanyGoalDto> Save(SaveGoalCommand command, CancellationToken cancellationToken) =>
        sender.Send(command, cancellationToken);
}

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
