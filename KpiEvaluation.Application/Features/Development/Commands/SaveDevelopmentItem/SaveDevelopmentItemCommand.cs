using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Development.Commands.SaveDevelopmentItem;

public sealed record SaveDevelopmentItemCommand(DevelopmentItemSaveInput Input) : IRequest<DevelopmentItemDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.DevelopmentManage;
}

