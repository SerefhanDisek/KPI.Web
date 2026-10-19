using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Pages.Queries.GetPageDefinitions;

public sealed class GetPageDefinitionsQueryHandler(IPageDefinitionRepository repository) : IRequestHandler<GetPageDefinitionsQuery, IReadOnlyList<PageDefinitionDto>>
{
    public Task<IReadOnlyList<PageDefinitionDto>> Handle(GetPageDefinitionsQuery request, CancellationToken cancellationToken) => repository.GetAllAsync(cancellationToken);
}
