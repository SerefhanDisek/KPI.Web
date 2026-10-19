using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Goals.Commands.SaveGoal;

public sealed class SaveGoalCommandHandler(ICompanyGoalRepository repository) : IRequestHandler<SaveGoalCommand, CompanyGoalDto>
{
    public Task<CompanyGoalDto> Handle(SaveGoalCommand request, CancellationToken cancellationToken) =>
        repository.SaveAsync(request.Id, request.Name, request.Description, request.EvaluationPeriodId, request.Priority,
            request.IsActive, cancellationToken);
}
