using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.DeactivateRole;

public sealed class DeactivateRoleCommandHandler(IAccessControlRepository repository) : IRequestHandler<DeactivateRoleCommand>
{
    public Task Handle(DeactivateRoleCommand request, CancellationToken cancellationToken) =>
        repository.DeactivateRoleAsync(request.Id, cancellationToken);
}
