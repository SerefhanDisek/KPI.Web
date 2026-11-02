using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Periods.Commands.DeletePeriod;

public sealed record DeletePeriodCommand(int Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.PeriodManage;
}
