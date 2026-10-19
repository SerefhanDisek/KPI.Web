using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Employees.Queries.GetEmployees;

public sealed record GetEmployeesQuery(bool ActiveOnly) : IRequest<IReadOnlyList<EmployeeDto>>, IAnyAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.EmployeeManage, AppPermission.EvaluationCreate, AppPermission.EvaluationHistory, AppPermission.LeaveView];
}
