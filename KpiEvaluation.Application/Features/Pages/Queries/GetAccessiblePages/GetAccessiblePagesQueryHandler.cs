using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Pages.Queries.GetAccessiblePages;

public sealed class GetAccessiblePagesQueryHandler(ICurrentUser currentUser, IPageDefinitionRepository repository) : IRequestHandler<GetAccessiblePagesQuery, IReadOnlyList<PageDefinitionDto>>
{
    public Task<IReadOnlyList<PageDefinitionDto>> Handle(GetAccessiblePagesQuery request, CancellationToken cancellationToken) => repository.GetAccessibleAsync(currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}
