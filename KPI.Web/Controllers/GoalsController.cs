using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Goals.Commands.DeactivateGoal;
using KpiEvaluation.Application.Features.Goals.Commands.DeleteGoal;
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

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeactivateGoalCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteGoalCommand(id), cancellationToken);
        return NoContent();
    }
}
