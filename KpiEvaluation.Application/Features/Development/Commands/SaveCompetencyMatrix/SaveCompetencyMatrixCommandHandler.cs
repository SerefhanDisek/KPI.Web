using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Development.Commands.SaveCompetencyMatrix;

public sealed class SaveCompetencyMatrixCommandHandler(IDevelopmentRepository repository) : IRequestHandler<SaveCompetencyMatrixCommand, CompetencyMatrixEntryDto>
{
    public Task<CompetencyMatrixEntryDto> Handle(SaveCompetencyMatrixCommand request, CancellationToken cancellationToken) =>
        repository.SaveMatrixAsync(request.Input, cancellationToken);
}

