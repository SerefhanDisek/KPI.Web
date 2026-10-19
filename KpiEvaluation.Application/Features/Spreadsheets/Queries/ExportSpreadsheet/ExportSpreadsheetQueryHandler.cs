using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Spreadsheets.Queries.ExportSpreadsheet;

public sealed class ExportSpreadsheetQueryHandler(ISpreadsheetRepository repository, ICurrentUser currentUser)
    : IRequestHandler<ExportSpreadsheetQuery, SpreadsheetFileDto>
{
    public Task<SpreadsheetFileDto> Handle(ExportSpreadsheetQuery request, CancellationToken cancellationToken) =>
        repository.ExportAsync(request.Resource, request.Input, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}
