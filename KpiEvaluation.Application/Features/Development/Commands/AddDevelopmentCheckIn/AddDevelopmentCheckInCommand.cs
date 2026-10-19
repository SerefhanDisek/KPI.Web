using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Development.Commands.AddDevelopmentCheckIn;

public sealed record AddDevelopmentCheckInCommand(long Id, string Note, decimal Progress, int ExpectedVersion) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.DevelopmentManage;
}

