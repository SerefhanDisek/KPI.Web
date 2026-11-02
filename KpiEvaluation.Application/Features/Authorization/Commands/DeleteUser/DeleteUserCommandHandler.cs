using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(IAccessControlRepository repository) : IRequestHandler<DeleteUserCommand>
{
    public Task Handle(DeleteUserCommand request, CancellationToken cancellationToken) =>
        repository.DeleteUserAsync(request.Id, cancellationToken);
}
