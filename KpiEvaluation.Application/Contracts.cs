using KpiEvaluation.Domain;

namespace KpiEvaluation.Application;

public sealed record LookupDto(int Id, string Name);
public sealed record ProfessionDto(int Id, string Name, bool IsActive);
public sealed record EmployeeDto(int Id, string FullName, int ProfessionId, string ProfessionName, bool IsActive,
    int? FirstManagerUserId = null, string? FirstManagerName = null, int? SecondManagerUserId = null, string? SecondManagerName = null);
public sealed record KpiDto(int Id, string Title, string? Description, int? ProfessionId, string? ProfessionName, bool IsCommon, int DisplayOrder, bool IsActive,
    string? Category = null, int? CompanyGoalId = null, string? CompanyGoalName = null,
    KpiMeasurementType MeasurementType = KpiMeasurementType.ManualScore, string? Unit = null, decimal? TargetValue = null,
    KpiTargetDirection TargetDirection = KpiTargetDirection.AtLeast, decimal WeightPercent = 0, string? DataSource = null,
    KpiReviewFrequency ReviewFrequency = KpiReviewFrequency.SemiAnnual, DateOnly? ValidFrom = null, DateOnly? ValidTo = null,
    bool IsSpecific = false, bool IsMeasurable = false, bool IsAchievable = false, bool IsRelevant = false, bool IsTimeBound = false,
    IReadOnlyList<LookupDto>? Professions = null)
{
    public string MeasurementName => MeasurementType switch { KpiMeasurementType.ManualScore=>"Manuel 1-5",KpiMeasurementType.Percentage=>"Yüzde",KpiMeasurementType.Count=>"Adet",KpiMeasurementType.Duration=>"Süre",_=>"Sayısal" };
}
public sealed record PeriodDto(int Id, string Name, DateOnly StartDate, DateOnly EndDate, bool IsActive);
public sealed record CompanyGoalDto(int Id, string Name, string? Description, int? EvaluationPeriodId, string? PeriodName, int Priority, bool IsActive);
public sealed record EvaluationItemInput(int KpiCriterionId, int Score, string? Note, decimal? ActualValue = null);
public sealed record EvaluationSaveInput(int EmployeeId,int EvaluationPeriodId,string? GeneralNote,IReadOnlyList<EvaluationItemInput> Items,DateTime? EvaluationDate=null,
    EvaluationStatus Status=EvaluationStatus.Completed,string? ManagerFeedback=null,string? EmployeeFeedback=null,string? DevelopmentAction=null,DateOnly? FollowUpDate=null);
public sealed record EvaluationSummaryDto(long Id, string EmployeeName, string ProfessionName, string PeriodName, int TotalScore, decimal AverageScore, DateTime EvaluationDate, string? GeneralNote,
    decimal WeightedScore=0,EvaluationStatus Status=EvaluationStatus.Completed,string? ManagerFeedback=null,string? EmployeeFeedback=null,string? DevelopmentAction=null,DateOnly? FollowUpDate=null,
    int MaximumScore=0,decimal ScorePercentage=0,int? EvaluatorUserId=null,string? EvaluatorName=null,DateTime? ApprovedAt=null,
    bool CanEdit=false,bool CanApprove=false)
{
    public string StatusName => Status switch { EvaluationStatus.Draft=>"Taslak",EvaluationStatus.Completed=>"Tamamlandı",EvaluationStatus.Shared=>"Çalışana İletildi",EvaluationStatus.EmployeeCommented=>"Çalışan Görüşü Alındı",_=>"Onaylandı" };
}
public sealed record EvaluationDetailDto(string KpiTitle, int Score, string? Note, bool IsCommon, decimal? ActualValue = null,
    decimal WeightPercent = 0, decimal? TargetValue = null, KpiMeasurementType MeasurementType = KpiMeasurementType.ManualScore, string? Unit = null, decimal WeightedScore = 0, KpiTargetDirection TargetDirection = KpiTargetDirection.AtLeast);
public sealed record EvaluationEditItemDto(long DetailId, string KpiTitle, int Score, string? Note, bool IsCommon, decimal? ActualValue = null,
    KpiMeasurementType MeasurementType = KpiMeasurementType.ManualScore, string? Unit = null, decimal? TargetValue = null, KpiTargetDirection TargetDirection = KpiTargetDirection.AtLeast);
public sealed record EvaluationEditDto(long Id, string? GeneralNote, IReadOnlyList<EvaluationEditItemDto> Items,
    EvaluationStatus Status = EvaluationStatus.Completed, string? ManagerFeedback = null, string? EmployeeFeedback = null,
    string? DevelopmentAction = null, DateOnly? FollowUpDate = null);
public sealed record EvaluationItemUpdateInput(long DetailId, int Score, string? Note, decimal? ActualValue = null);
public sealed record GoalProgressDto(string GoalName,decimal AverageScore,int EvaluationCount);
public sealed record DashboardDto(int TotalEmployees,int EvaluatedEmployees,int NotEvaluatedEmployees,decimal OverallAverage,IReadOnlyList<EvaluationSummaryDto> Results,IReadOnlyList<GoalProgressDto>? GoalProgress=null,
    decimal OverallPercentage=0,IReadOnlyList<ManagerDashboardDto>? Managers=null);
public sealed record PortableEvaluationItemDto(string KpiTitle,int Score,string? Note,bool IsCommon,decimal? ActualValue=null);
public sealed record PortableEvaluationDto(string EmployeeName,string ProfessionName,string PeriodName,DateTime EvaluationDate,string? GeneralNote,IReadOnlyList<PortableEvaluationItemDto> Items,
    EvaluationStatus Status=EvaluationStatus.Completed,string? ManagerFeedback=null,string? EmployeeFeedback=null,string? DevelopmentAction=null,DateOnly? FollowUpDate=null);
public sealed record ImportResultDto(int ImportedCount, int SkippedCount, IReadOnlyList<string> Errors);

public interface IDatabaseMaintenanceService
{
    Task BackupAsync(string filePath, CancellationToken cancellationToken = default);
    Task RestoreAsync(string filePath, CancellationToken cancellationToken = default);
}

public sealed class ValidationException(string message) : Exception(message);
public sealed class DuplicateEvaluationException() : Exception("Bu çalışan için seçilen dönemde zaten bir değerlendirme bulunmaktadır.");
