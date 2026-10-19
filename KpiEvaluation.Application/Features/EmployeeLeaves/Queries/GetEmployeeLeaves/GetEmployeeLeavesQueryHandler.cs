using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.EmployeeLeaves.Queries.GetEmployeeLeaves;

public sealed class GetEmployeeLeavesQueryHandler(IEmployeeLeaveRepository repository) : IRequestHandler<GetEmployeeLeavesQuery, EmployeeLeavePageDto>
{
    public Task<EmployeeLeavePageDto> Handle(GetEmployeeLeavesQuery request, CancellationToken cancellationToken) =>
        repository.GetAsync(request.EmployeeId, request.ProfessionId, request.From, request.To,
            request.ActiveOnly, request.Page, request.PageSize, cancellationToken);
}
