using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authentication.Commands.Login;

public sealed class LoginCommandHandler(IAuthenticationRepository repository) : IRequestHandler<LoginCommand, AuthenticatedUserDto?>
{
    public Task<AuthenticatedUserDto?> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        repository.AuthenticateAsync(request.UserName, request.Password, cancellationToken);
}
