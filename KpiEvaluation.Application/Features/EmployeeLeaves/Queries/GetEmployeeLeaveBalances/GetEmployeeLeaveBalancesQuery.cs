using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.EmployeeLeaves.Queries.GetEmployeeLeaveBalances;

public sealed record GetEmployeeLeaveBalancesQuery(int? EmployeeId, int? ProfessionId, DateOnly? From, DateOnly? To,
    bool ActiveOnly = true, int Page = 1, int PageSize = 25) : IRequest<EmployeeLeaveBalancePageDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.LeaveView;
}
