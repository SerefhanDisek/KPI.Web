using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Pages.Commands.SavePageDefinition;

public sealed class SavePageDefinitionCommandHandler(IPageDefinitionRepository repository) : IRequestHandler<SavePageDefinitionCommand, PageDefinitionDto>
{
    public Task<PageDefinitionDto> Handle(SavePageDefinitionCommand request, CancellationToken cancellationToken) => repository.SaveAsync(request.Id, request.Title, request.Slug, request.Description, request.LayoutJson, request.RequiredPermission, request.IsActive, cancellationToken);
}
