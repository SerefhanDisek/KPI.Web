namespace KpiEvaluation.Application.Repositories;

public interface ISpreadsheetRepository
{
    Task<SpreadsheetFileDto> ExportAsync(SpreadsheetResource resource, SpreadsheetExportInput input, int userId,
        CancellationToken cancellationToken = default);
    Task<ImportResultDto> ImportAsync(SpreadsheetResource resource, Stream content, string fileName, bool permanentEvaluations,
        int userId, CancellationToken cancellationToken = default);
}
