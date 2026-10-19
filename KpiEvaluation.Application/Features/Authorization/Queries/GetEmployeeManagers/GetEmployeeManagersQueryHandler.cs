using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Queries.GetEmployeeManagers;

public sealed class GetEmployeeManagersQueryHandler(IAccessControlRepository repository) : IRequestHandler<GetEmployeeManagersQuery, IReadOnlyList<EmployeeManagerDto>>
{
    public Task<IReadOnlyList<EmployeeManagerDto>> Handle(GetEmployeeManagersQuery request, CancellationToken cancellationToken) => repository.GetEmployeeManagersAsync(cancellationToken);
}
