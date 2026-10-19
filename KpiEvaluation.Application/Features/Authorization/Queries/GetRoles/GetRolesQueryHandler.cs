using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Queries.GetRoles;

public sealed class GetRolesQueryHandler(IAccessControlRepository repository) : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken) => repository.GetRolesAsync(cancellationToken);
}
