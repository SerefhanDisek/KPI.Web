using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Development.Queries.GetDevelopmentBoard;

public sealed record GetDevelopmentBoardQuery(int? EmployeeId = null, bool ActiveOnly = true, int Page = 1, int PageSize = 50) : IRequest<DevelopmentBoardDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.DevelopmentManage;
}

