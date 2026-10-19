using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Professions.Queries.GetProfessionLookups;

public sealed record GetProfessionLookupsQuery : IRequest<IReadOnlyList<LookupDto>>, IAnyAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.ProfessionManage, AppPermission.EvaluationCreate];
}
