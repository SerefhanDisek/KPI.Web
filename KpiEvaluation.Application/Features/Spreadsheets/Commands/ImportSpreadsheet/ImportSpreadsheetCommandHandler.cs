using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Spreadsheets.Commands.ImportSpreadsheet;

public sealed class ImportSpreadsheetCommandHandler(ISpreadsheetRepository repository, ICurrentUser currentUser)
    : IRequestHandler<ImportSpreadsheetCommand, ImportResultDto>
{
    public Task<ImportResultDto> Handle(ImportSpreadsheetCommand request, CancellationToken cancellationToken) =>
        repository.ImportAsync(request.Resource, request.Content, request.FileName, request.PermanentEvaluations,
            currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}
