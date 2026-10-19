using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.EmployeeLeaves.Commands.SaveEmployeeLeave;

public sealed class SaveEmployeeLeaveCommandHandler(IEmployeeLeaveRepository repository, ICurrentUser currentUser)
    : IRequestHandler<SaveEmployeeLeaveCommand, EmployeeLeaveDto>
{
    public Task<EmployeeLeaveDto> Handle(SaveEmployeeLeaveCommand request, CancellationToken cancellationToken) =>
        repository.SaveAsync(request.Input, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}
