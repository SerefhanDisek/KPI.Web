using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.SaveRole;

public sealed class SaveRoleCommandHandler(IAccessControlRepository repository) : IRequestHandler<SaveRoleCommand, RoleDto>
{
    public Task<RoleDto> Handle(SaveRoleCommand request, CancellationToken cancellationToken) => repository.SaveRoleAsync(request.Id, request.Name, request.Code, request.IsActive, cancellationToken);
}
