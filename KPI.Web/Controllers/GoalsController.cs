using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Goals.Commands.SaveGoal;
using KpiEvaluation.Application.Features.Goals.Queries.GetGoals;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

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
