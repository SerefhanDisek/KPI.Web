using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Employees.Commands.SaveEmployee;

public sealed class SaveEmployeeCommandHandler(IEmployeeRepository repository) : IRequestHandler<SaveEmployeeCommand, EmployeeDto>
{
    public Task<EmployeeDto> Handle(SaveEmployeeCommand request, CancellationToken cancellationToken) =>
        repository.SaveAsync(request.Id, request.FullName, request.ProfessionId, request.IsActive, cancellationToken);
}
