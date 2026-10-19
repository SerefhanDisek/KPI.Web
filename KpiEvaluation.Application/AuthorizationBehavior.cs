using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application;

public sealed class AuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser, IAccessControlRepository accessControl)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IAuthorizedRequest && request is not IAnyAuthorizedRequest && request is not IAllAuthorizedRequest) return await next();
        if (!currentUser.IsAuthenticated || currentUser.UserId is null) throw new AuthorizationException();
        var allowed = request switch
        {
            IAuthorizedRequest single => await accessControl.HasPermissionAsync(currentUser.UserId.Value, single.Permission, cancellationToken),
            IAnyAuthorizedRequest any => await HasAnyAsync(currentUser.UserId.Value, any.Permissions, cancellationToken),
            IAllAuthorizedRequest all => await HasAllAsync(currentUser.UserId.Value, all.Permissions, cancellationToken),
            _ => true
        };
        if (!allowed) throw new AuthorizationException();

        return await next();
    }

    private async Task<bool> HasAnyAsync(int userId, IEnumerable<KpiEvaluation.Domain.AppPermission> permissions, CancellationToken cancellationToken)
    {
        foreach (var permission in permissions)
            if (await accessControl.HasPermissionAsync(userId, permission, cancellationToken)) return true;
        return false;
    }

    private async Task<bool> HasAllAsync(int userId, IEnumerable<KpiEvaluation.Domain.AppPermission> permissions, CancellationToken cancellationToken)
    {
        foreach (var permission in permissions)
            if (!await accessControl.HasPermissionAsync(userId, permission, cancellationToken)) return false;
        return true;
    }
}
