using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Professions.Commands.DeleteProfession;

public sealed record DeleteProfessionCommand(int Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.ProfessionManage;
}
