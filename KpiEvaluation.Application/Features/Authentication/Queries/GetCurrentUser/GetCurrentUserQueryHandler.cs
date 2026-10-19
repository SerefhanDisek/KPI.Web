using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authentication.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(ICurrentUser currentUser, IAccessControlRepository repository)
    : IRequestHandler<GetCurrentUserQuery, AuthenticatedUserDto>
{
    public Task<AuthenticatedUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken) =>
        repository.GetCurrentAsync(currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}
