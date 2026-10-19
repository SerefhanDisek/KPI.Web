using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.SaveRolePermissions;

public sealed class SaveRolePermissionsCommandHandler(IAccessControlRepository repository) : IRequestHandler<SaveRolePermissionsCommand>
{
    public Task Handle(SaveRolePermissionsCommand request, CancellationToken cancellationToken) => repository.SaveRolePermissionsAsync(request.RoleId, request.Permissions, cancellationToken);
}
