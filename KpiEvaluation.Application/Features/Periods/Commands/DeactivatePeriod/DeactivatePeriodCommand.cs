using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Periods.Commands.DeactivatePeriod;

public sealed record DeactivatePeriodCommand(int Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.PeriodManage;
}
