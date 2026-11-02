using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Employees.Commands.DeactivateEmployee;

public sealed record DeactivateEmployeeCommand(int Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EmployeeManage;
}
