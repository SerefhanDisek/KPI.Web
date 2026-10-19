using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Queries.GetEmployeeManagers;

public sealed record GetEmployeeManagersQuery : IRequest<IReadOnlyList<EmployeeManagerDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
