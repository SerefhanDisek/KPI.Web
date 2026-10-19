using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Development.Queries.GetDevelopmentCheckIns;

public sealed record GetDevelopmentCheckInsQuery(long Id) : IRequest<IReadOnlyList<DevelopmentCheckInDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.DevelopmentManage;
}

