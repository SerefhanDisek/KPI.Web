using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Goals.Queries.GetGoals;

public sealed class GetGoalsQueryHandler(ICompanyGoalRepository repository) : IRequestHandler<GetGoalsQuery, IReadOnlyList<CompanyGoalDto>>
{
    public Task<IReadOnlyList<CompanyGoalDto>> Handle(GetGoalsQuery request, CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);
}
