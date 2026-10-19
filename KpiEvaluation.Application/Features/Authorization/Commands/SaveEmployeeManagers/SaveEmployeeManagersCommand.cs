using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Authorization.Commands.SaveEmployeeManagers;

public sealed record SaveEmployeeManagersCommand(int EmployeeId, int? FirstManagerUserId, int? SecondManagerUserId) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuthorizationManage;
}
