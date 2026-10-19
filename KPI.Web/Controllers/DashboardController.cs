using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Dashboard.Queries.GetDashboard;
using KpiEvaluation.Application.Features.Dashboard.Queries.GetManagerDashboard;
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
