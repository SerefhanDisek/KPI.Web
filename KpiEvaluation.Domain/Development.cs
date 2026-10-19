namespace KpiEvaluation.Domain;

public sealed class CompetencyMatrixEntry
{
    public int Id { get; set; }
    public int ProfessionId { get; set; }
    public Profession Profession { get; set; } = null!;
    public required string Seniority { get; set; }
    public required string Competency { get; set; }
    public required string ExpectedLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
}

public enum DevelopmentItemKind { Goal = 0, DevelopmentPlan = 1 }

public sealed class DevelopmentItem
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DevelopmentItemKind Kind { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateOnly? TargetDate { get; set; }
    public decimal Progress { get; set; }
    public bool IsActive { get; set; } = true;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int Version { get; set; } = 1;
    public ICollection<DevelopmentCheckIn> CheckIns { get; set; } = [];
}

public sealed class DevelopmentCheckIn
{
    public long Id { get; set; }
    public long DevelopmentItemId { get; set; }
    public DevelopmentItem DevelopmentItem { get; set; } = null!;
    public required string Note { get; set; }
    public decimal Progress { get; set; }
    public int CreatedByUserId { get; set; }
    public AppUser CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
