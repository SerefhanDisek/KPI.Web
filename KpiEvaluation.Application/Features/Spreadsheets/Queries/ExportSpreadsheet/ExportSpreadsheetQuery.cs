using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Spreadsheets.Queries.ExportSpreadsheet;

public sealed record ExportSpreadsheetQuery(SpreadsheetResource Resource, SpreadsheetExportInput Input)
    : IRequest<SpreadsheetFileDto>, IAllAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => Resource switch
    {
        SpreadsheetResource.Employees => [AppPermission.EmployeeManage],
        SpreadsheetResource.Kpis => [AppPermission.KpiManage],
        SpreadsheetResource.Leaves => [AppPermission.LeaveView],
        SpreadsheetResource.Evaluations when Input.Template => [AppPermission.EvaluationHistory, AppPermission.EvaluationCreate],
        SpreadsheetResource.Evaluations => [AppPermission.EvaluationHistory],
        _ => throw new ValidationException("Geçersiz Excel sayfası.")
    };
}
