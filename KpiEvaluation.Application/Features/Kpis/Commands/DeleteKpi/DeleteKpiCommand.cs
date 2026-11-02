using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Kpis.Commands.DeleteKpi;

public sealed record DeleteKpiCommand(int Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.KpiManage;
}
