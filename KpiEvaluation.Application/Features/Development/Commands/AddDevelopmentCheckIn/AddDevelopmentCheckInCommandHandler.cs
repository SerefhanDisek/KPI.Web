using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Development.Commands.AddDevelopmentCheckIn;

public sealed class AddDevelopmentCheckInCommandHandler(IDevelopmentRepository repository, ICurrentUser currentUser) : IRequestHandler<AddDevelopmentCheckInCommand>
{
    public Task Handle(AddDevelopmentCheckInCommand request, CancellationToken cancellationToken) =>
        repository.AddCheckInAsync(request.Id, request.Note, request.Progress, request.ExpectedVersion, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}

