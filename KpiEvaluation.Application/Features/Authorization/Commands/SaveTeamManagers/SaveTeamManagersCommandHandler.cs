using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.SaveTeamManagers;

public sealed class SaveTeamManagersCommandHandler(IAccessControlRepository repository) : IRequestHandler<SaveTeamManagersCommand>
{
    public Task Handle(SaveTeamManagersCommand request, CancellationToken cancellationToken) => repository.SaveTeamManagersAsync(request.ProfessionId, request.Managers, cancellationToken);
}
