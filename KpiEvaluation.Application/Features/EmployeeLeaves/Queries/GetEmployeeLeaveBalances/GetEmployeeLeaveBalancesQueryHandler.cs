using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.EmployeeLeaves.Queries.GetEmployeeLeaveBalances;

public sealed class GetEmployeeLeaveBalancesQueryHandler(IEmployeeLeaveRepository repository) : IRequestHandler<GetEmployeeLeaveBalancesQuery, EmployeeLeaveBalancePageDto>
{
    public Task<EmployeeLeaveBalancePageDto> Handle(GetEmployeeLeaveBalancesQuery request, CancellationToken cancellationToken) =>
        repository.GetBalancesAsync(request.EmployeeId, request.ProfessionId, request.From, request.To,
            request.ActiveOnly, request.Page, request.PageSize, cancellationToken);
}
