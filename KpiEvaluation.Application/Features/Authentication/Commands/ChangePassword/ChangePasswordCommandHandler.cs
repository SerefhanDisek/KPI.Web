using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authentication.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(ICurrentUser currentUser, IAuthenticationRepository repository) : IRequestHandler<ChangePasswordCommand>
{
    public Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken) =>
        repository.ChangePasswordAsync(currentUser.UserId ?? throw new AuthorizationException(), request.CurrentPassword,
            request.NewPassword, cancellationToken);
}
