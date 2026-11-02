using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Professions.Commands.DeactivateProfession;

public sealed record DeactivateProfessionCommand(int Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.ProfessionManage;
}
