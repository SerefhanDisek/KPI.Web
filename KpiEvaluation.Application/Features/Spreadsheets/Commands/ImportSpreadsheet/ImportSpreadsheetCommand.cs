using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Spreadsheets.Commands.ImportSpreadsheet;

public sealed record ImportSpreadsheetCommand(SpreadsheetResource Resource, Stream Content, string FileName, bool PermanentEvaluations)
    : IRequest<ImportResultDto>, IAllAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => Resource switch
    {
        SpreadsheetResource.Employees => [AppPermission.EmployeeManage],
        SpreadsheetResource.Kpis => [AppPermission.KpiManage],
        SpreadsheetResource.Leaves => [AppPermission.LeaveView, AppPermission.LeaveManage],
        SpreadsheetResource.Evaluations when PermanentEvaluations => [AppPermission.EvaluationHistory, AppPermission.EvaluationCreate, AppPermission.EvaluationApprove],
        SpreadsheetResource.Evaluations => [AppPermission.EvaluationHistory, AppPermission.EvaluationCreate],
        _ => throw new ValidationException("Geçersiz Excel sayfası.")
    };
}
