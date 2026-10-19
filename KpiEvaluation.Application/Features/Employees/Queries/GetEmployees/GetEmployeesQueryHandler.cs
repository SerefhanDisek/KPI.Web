using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Employees.Queries.GetEmployees;

public sealed class GetEmployeesQueryHandler(IEmployeeRepository repository) : IRequestHandler<GetEmployeesQuery, IReadOnlyList<EmployeeDto>>
{
    public Task<IReadOnlyList<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken) =>
        repository.GetAllAsync(request.ActiveOnly, cancellationToken);
}
