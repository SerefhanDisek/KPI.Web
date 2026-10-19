using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Kpis.Queries.GetKpisForProfession;

public sealed record GetKpisForProfessionQuery(int ProfessionId, int? EvaluationPeriodId) : IRequest<IReadOnlyList<KpiDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationCreate;
}
