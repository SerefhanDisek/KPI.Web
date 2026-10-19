using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Employees.Commands.SaveEmployee;

public sealed record SaveEmployeeCommand(int? Id, string FullName, int ProfessionId, bool IsActive) : IRequest<EmployeeDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EmployeeManage;
}
