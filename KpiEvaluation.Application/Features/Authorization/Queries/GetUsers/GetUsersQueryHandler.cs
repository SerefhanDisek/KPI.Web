using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Queries.GetUsers;

public sealed class GetUsersQueryHandler(IAccessControlRepository repository) : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken) => repository.GetUsersAsync(cancellationToken);
}
