using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Pages.Commands.SavePageDefinition;

public sealed record SavePageDefinitionCommand(int? Id, string Title, string Slug, string? Description, string LayoutJson,
    AppPermission RequiredPermission, bool IsActive) : IRequest<PageDefinitionDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.PageDesignerManage;
}
