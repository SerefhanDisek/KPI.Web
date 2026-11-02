using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Employees.Commands.DeleteEmployee;

public sealed record DeleteEmployeeCommand(int Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EmployeeManage;
}
