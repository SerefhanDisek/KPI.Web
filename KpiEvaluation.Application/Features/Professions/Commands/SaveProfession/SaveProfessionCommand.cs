using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Professions.Commands.SaveProfession;

public sealed record SaveProfessionCommand(int? Id, string Name, bool IsActive) : IRequest<ProfessionDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.ProfessionManage;
}
