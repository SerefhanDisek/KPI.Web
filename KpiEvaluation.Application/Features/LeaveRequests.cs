using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features;

public sealed record GetEmployeeLeavesQuery(int? EmployeeId, int? ProfessionId, DateOnly? From, DateOnly? To,
    bool ActiveOnly = true, int Page = 1, int PageSize = 25) : IRequest<EmployeeLeavePageDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.LeaveView;
}

public sealed record SaveEmployeeLeaveCommand(EmployeeLeaveSaveInput Input) : IRequest<EmployeeLeaveDto>, IAllAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.LeaveView, AppPermission.LeaveManage];
}
