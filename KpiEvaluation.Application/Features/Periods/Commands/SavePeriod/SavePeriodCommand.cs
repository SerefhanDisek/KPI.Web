using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Periods.Commands.SavePeriod;

public sealed record SavePeriodCommand(int? Id, string Name, DateOnly StartDate, DateOnly EndDate, bool IsActive) : IRequest<PeriodDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.PeriodManage;
}
