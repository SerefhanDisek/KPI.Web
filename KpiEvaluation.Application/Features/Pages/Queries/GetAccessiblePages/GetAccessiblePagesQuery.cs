using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Pages.Queries.GetAccessiblePages;

public sealed record GetAccessiblePagesQuery : IRequest<IReadOnlyList<PageDefinitionDto>>;
