using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Development.Queries.GetCompetencyMatrix;

public sealed record GetCompetencyMatrixQuery() : IRequest<CompetencyMatrixDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.TalentManage;
}

