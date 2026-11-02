using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Kpis.Commands.DeactivateKpi;

public sealed record DeactivateKpiCommand(int Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.KpiManage;
}
