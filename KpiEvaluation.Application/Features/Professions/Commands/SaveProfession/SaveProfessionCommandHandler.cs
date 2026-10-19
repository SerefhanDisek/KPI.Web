using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Professions.Commands.SaveProfession;

public sealed class SaveProfessionCommandHandler(IProfessionRepository repository) : IRequestHandler<SaveProfessionCommand, ProfessionDto>
{
    public Task<ProfessionDto> Handle(SaveProfessionCommand request, CancellationToken cancellationToken) =>
        repository.SaveAsync(request.Id, request.Name, request.IsActive, cancellationToken);
}
