using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Queries.GetRolePermissions;

public sealed class GetRolePermissionsQueryHandler(IAccessControlRepository repository) : IRequestHandler<GetRolePermissionsQuery, IReadOnlyList<RolePermissionDto>>
{
    public Task<IReadOnlyList<RolePermissionDto>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken) => repository.GetRolePermissionsAsync(cancellationToken);
}
