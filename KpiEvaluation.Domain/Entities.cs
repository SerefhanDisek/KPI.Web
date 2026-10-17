namespace KpiEvaluation.Domain;

public enum KpiMeasurementType { ManualScore = 0, Percentage = 1, Count = 2, Duration = 3, Numeric = 4 }
public enum KpiTargetDirection { AtLeast = 0, AtMost = 1 }
public enum KpiReviewFrequency { Monthly = 0, Quarterly = 1, SemiAnnual = 2, Annual = 3 }
public enum EvaluationStatus { Draft = 0, Completed = 1, Shared = 2, EmployeeCommented = 3, Approved = 4 }

public sealed class Profession
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Employee> Employees { get; set; } = [];
    public ICollection<KpiCriterion> KpiCriteria { get; set; } = [];
}

public sealed class Employee
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public int ProfessionId { get; set; }
    public Profession Profession { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? FirstManagerUserId { get; set; }
    public AppUser? FirstManagerUser { get; set; }
    public int? SecondManagerUserId { get; set; }
    public AppUser? SecondManagerUser { get; set; }
    public AppUser? UserAccount { get; set; }
    public ICollection<EmployeeEvaluation> Evaluations { get; set; } = [];
}

public sealed class KpiCriterion
{
    public int Id { get; set; }
    public int? ProfessionId { get; set; }
    public Profession? Profession { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int? CompanyGoalId { get; set; }
    public CompanyGoal? CompanyGoal { get; set; }
    public KpiMeasurementType MeasurementType { get; set; }
    public string? Unit { get; set; }
    public decimal? TargetValue { get; set; }
    public KpiTargetDirection TargetDirection { get; set; }
    public decimal WeightPercent { get; set; }
    public string? DataSource { get; set; }
    public KpiReviewFrequency ReviewFrequency { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsSpecific { get; set; }
    public bool IsMeasurable { get; set; }
    public bool IsAchievable { get; set; }
    public bool IsRelevant { get; set; }
    public bool IsTimeBound { get; set; }
    public bool IsCommon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<EmployeeEvaluationDetail> EvaluationDetails { get; set; } = [];
}

public sealed class CompanyGoal
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int? EvaluationPeriodId { get; set; }
    public EvaluationPeriod? EvaluationPeriod { get; set; }
    public int Priority { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<KpiCriterion> KpiCriteria { get; set; } = [];
}

public sealed class EvaluationPeriod
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<EmployeeEvaluation> Evaluations { get; set; } = [];
    public ICollection<CompanyGoal> CompanyGoals { get; set; } = [];
}

public sealed class EmployeeEvaluation
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public int EvaluationPeriodId { get; set; }
    public EvaluationPeriod EvaluationPeriod { get; set; } = null!;
    public required string EmployeeNameSnapshot { get; set; }
    public int ProfessionIdSnapshot { get; set; }
    public required string ProfessionNameSnapshot { get; set; }
    public required string PeriodNameSnapshot { get; set; }
    public DateTime EvaluationDate { get; set; } = DateTime.UtcNow;
    public int TotalScore { get; set; }
    public decimal AverageScore { get; set; }
    public decimal WeightedScore { get; set; }
    public int MaximumScore { get; set; }
    public decimal ScorePercentage { get; set; }
    public EvaluationStatus Status { get; set; } = EvaluationStatus.Completed;
    public int? EvaluatorUserId { get; set; }
    public AppUser? EvaluatorUser { get; set; }
    public int? ApprovedByUserId { get; set; }
    public AppUser? ApprovedByUser { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? GeneralNote { get; set; }
    public string? ManagerFeedback { get; set; }
    public string? EmployeeFeedback { get; set; }
    public string? DevelopmentAction { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<EmployeeEvaluationDetail> Details { get; set; } = [];
}

public sealed class EmployeeEvaluationDetail
{
    public long Id { get; set; }
    public long EmployeeEvaluationId { get; set; }
    public EmployeeEvaluation EmployeeEvaluation { get; set; } = null!;
    public int KpiCriterionId { get; set; }
    public KpiCriterion KpiCriterion { get; set; } = null!;
    public required string KpiTitleSnapshot { get; set; }
    public bool IsCommonSnapshot { get; set; }
    public int DisplayOrderSnapshot { get; set; }
    public int Score { get; set; }
    public decimal? ActualValue { get; set; }
    public decimal WeightPercentSnapshot { get; set; }
    public decimal? TargetValueSnapshot { get; set; }
    public KpiTargetDirection TargetDirectionSnapshot { get; set; }
    public KpiMeasurementType MeasurementTypeSnapshot { get; set; }
    public string? UnitSnapshot { get; set; }
    public decimal WeightedScore { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
