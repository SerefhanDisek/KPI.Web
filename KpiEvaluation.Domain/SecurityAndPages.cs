namespace KpiEvaluation.Domain;

public static class RoleCodes
{
    public const string Administrator = "administrator";
    public const string FirstManager = "first-manager";
    public const string SecondManager = "second-manager";
    public const string Employee = "employee";
    public const string HumanResources = "human-resources";
}

public enum AppPermission
{
    DashboardView = 0,
    EvaluationCreate = 1,
    EvaluationApprove = 2,
    EvaluationHistory = 3,
    EmployeeManage = 4,
    ProfessionManage = 5,
    KpiManage = 6,
    PeriodManage = 7,
    GoalManage = 8,
    PageDesignerManage = 9,
    AuthorizationManage = 10,
    LeaveView = 11,
    LeaveManage = 12
}

public sealed class AppRole
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<AppUser> Users { get; set; } = [];
    public ICollection<RolePermission> Permissions { get; set; } = [];
}

public sealed class AppUser
{
    public int Id { get; set; }
    public required string UserName { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public int RoleId { get; set; }
    public AppRole Role { get; set; } = null!;
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public sealed class RolePermission
{
    public int RoleId { get; set; }
    public AppRole Role { get; set; } = null!;
    public AppPermission Permission { get; set; }
    public bool IsAllowed { get; set; }
}

public sealed class PageDefinition
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public required string LayoutJson { get; set; }
    public AppPermission RequiredPermission { get; set; } = AppPermission.DashboardView;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
