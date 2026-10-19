using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Spreadsheets.Commands.ImportSpreadsheet;
using KpiEvaluation.Application.Features.Spreadsheets.Queries.ExportSpreadsheet;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/spreadsheets/{resource}")]
public sealed class SpreadsheetsController(ISender sender) : ControllerBase
{
    [HttpPost("export")]
    public async Task<IActionResult> Export(SpreadsheetResource resource, SpreadsheetExportInput input, CancellationToken cancellationToken)
    {
        var file = await sender.Send(new ExportSpreadsheetQuery(resource, input), cancellationToken);
        return File(file.Content, SpreadsheetFileDto.ContentType, file.FileName);
    }

    [HttpPost("import")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    public async Task<ImportResultDto> Import(SpreadsheetResource resource, IFormFile file,
        CancellationToken cancellationToken, [FromForm] bool permanentEvaluations = false)
    {
        await using var content = file.OpenReadStream();
        return await sender.Send(new ImportSpreadsheetCommand(resource, content, file.FileName, permanentEvaluations), cancellationToken);
    }
}
