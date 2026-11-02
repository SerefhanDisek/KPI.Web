using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Professions.Commands.DeleteProfession;

public sealed class DeleteProfessionCommandHandler(IProfessionRepository repository) : IRequestHandler<DeleteProfessionCommand>
{
    public Task Handle(DeleteProfessionCommand request, CancellationToken cancellationToken) =>
        repository.DeleteAsync(request.Id, cancellationToken);
}
