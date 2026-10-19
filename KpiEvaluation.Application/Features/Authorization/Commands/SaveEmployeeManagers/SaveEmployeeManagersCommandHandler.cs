using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.SaveEmployeeManagers;

public sealed class SaveEmployeeManagersCommandHandler(IAccessControlRepository repository) : IRequestHandler<SaveEmployeeManagersCommand>
{
    public Task Handle(SaveEmployeeManagersCommand request, CancellationToken cancellationToken) => repository.SaveEmployeeManagersAsync(request.EmployeeId, request.FirstManagerUserId, request.SecondManagerUserId, cancellationToken);
}
