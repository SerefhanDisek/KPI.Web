using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Kpis.Queries.GetKpis;

public sealed record GetKpisQuery : IRequest<IReadOnlyList<KpiDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.KpiManage;
}
