using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Pages.Queries.GetPageDefinitions;

public sealed record GetPageDefinitionsQuery : IRequest<IReadOnlyList<PageDefinitionDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.PageDesignerManage;
}
