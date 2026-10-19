using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.EmployeeLeaves.Queries.GetEmployeeLeaves;

public sealed record GetEmployeeLeavesQuery(int? EmployeeId, int? ProfessionId, DateOnly? From, DateOnly? To,
    bool ActiveOnly = true, int Page = 1, int PageSize = 25) : IRequest<EmployeeLeavePageDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.LeaveView;
}
