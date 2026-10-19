using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.EmployeeLeaves.Commands.SaveEmployeeLeave;

public sealed record SaveEmployeeLeaveCommand(EmployeeLeaveSaveInput Input) : IRequest<EmployeeLeaveDto>, IAllAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.LeaveView, AppPermission.LeaveManage];
}
