namespace KpiEvaluation.Domain;

public sealed class EmployeeLeave
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public required string LeaveType { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
    public int CreatedByUserId { get; set; }
    public AppUser CreatedByUser { get; set; } = null!;
    public int? UpdatedByUserId { get; set; }
    public AppUser? UpdatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
