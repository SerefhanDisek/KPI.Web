using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.DeleteRole;

public sealed class DeleteRoleCommandHandler(IAccessControlRepository repository) : IRequestHandler<DeleteRoleCommand>
{
    public Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken) =>
        repository.DeleteRoleAsync(request.Id, cancellationToken);
}
