using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Development.Commands.SaveCompetencyMatrix;

public sealed record SaveCompetencyMatrixCommand(CompetencyMatrixSaveInput Input) : IRequest<CompetencyMatrixEntryDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.TalentManage;
}

