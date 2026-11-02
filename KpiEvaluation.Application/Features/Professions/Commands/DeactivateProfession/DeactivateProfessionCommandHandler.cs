using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Professions.Commands.DeactivateProfession;

public sealed class DeactivateProfessionCommandHandler(IProfessionRepository repository) : IRequestHandler<DeactivateProfessionCommand>
{
    public Task Handle(DeactivateProfessionCommand request, CancellationToken cancellationToken) =>
        repository.DeactivateAsync(request.Id, cancellationToken);
}
