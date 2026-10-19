using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Periods.Commands.SavePeriod;

public sealed class SavePeriodCommandHandler(IEvaluationPeriodRepository repository) : IRequestHandler<SavePeriodCommand, PeriodDto>
{
    public Task<PeriodDto> Handle(SavePeriodCommand request, CancellationToken cancellationToken) =>
        repository.SaveAsync(request.Id, request.Name, request.StartDate, request.EndDate, request.IsActive, cancellationToken);
}
