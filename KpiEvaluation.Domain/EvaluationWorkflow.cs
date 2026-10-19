namespace KpiEvaluation.Domain;

public enum EmployeeResponseDecision { Accepted = 0, Comment = 2 }

public sealed class EvaluationEmployeeResponse
{
    public long Id { get; set; }
    public long EvaluationId { get; set; }
    public EmployeeEvaluation Evaluation { get; set; } = null!;
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public EmployeeResponseDecision Decision { get; set; }
    public required string Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AuditEntry
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public required string EntityType { get; set; }
    public required string EntityId { get; set; }
    public required string Action { get; set; }
    public required string ChangedFieldsJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
