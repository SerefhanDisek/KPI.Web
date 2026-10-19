using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.SaveUser;

public sealed class SaveUserCommandHandler(IAccessControlRepository repository) : IRequestHandler<SaveUserCommand, UserDto>
{
    public Task<UserDto> Handle(SaveUserCommand request, CancellationToken cancellationToken) => repository.SaveUserAsync(request.Input, cancellationToken);
}
