using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.DeactivateUser;

public sealed class DeactivateUserCommandHandler(IAccessControlRepository repository) : IRequestHandler<DeactivateUserCommand>
{
    public Task Handle(DeactivateUserCommand request, CancellationToken cancellationToken) =>
        repository.DeactivateUserAsync(request.Id, cancellationToken);
}
