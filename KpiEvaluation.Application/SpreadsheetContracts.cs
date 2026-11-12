namespace KpiEvaluation.Application;

public enum SpreadsheetResource { Employees, Kpis, Leaves, Evaluations, Calibration }

public sealed record SpreadsheetExportInput(bool Template = false, IReadOnlyList<long>? Ids = null,
    int? EmployeeId = null, int? ProfessionId = null, DateOnly? From = null, DateOnly? To = null, bool ActiveOnly = true,
    IReadOnlyList<int>? PeriodIds = null);
public sealed record SpreadsheetFileDto(byte[] Content, string FileName)
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
