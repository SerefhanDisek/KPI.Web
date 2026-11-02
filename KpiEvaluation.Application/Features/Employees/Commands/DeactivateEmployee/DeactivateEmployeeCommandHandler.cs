using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Employees.Commands.DeactivateEmployee;

public sealed class DeactivateEmployeeCommandHandler(IEmployeeRepository repository) : IRequestHandler<DeactivateEmployeeCommand>
{
    public Task Handle(DeactivateEmployeeCommand request, CancellationToken cancellationToken) =>
        repository.DeactivateAsync(request.Id, cancellationToken);
}
