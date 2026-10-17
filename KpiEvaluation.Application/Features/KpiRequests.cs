using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features;

public sealed record GetDashboardQuery(int? PeriodId, int? EvaluatorUserId = null) : IRequest<DashboardDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.DashboardView;
}
public sealed record GetEmployeesQuery(bool ActiveOnly) : IRequest<IReadOnlyList<EmployeeDto>>, IAnyAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.EmployeeManage, AppPermission.EvaluationCreate, AppPermission.EvaluationHistory];
}
public sealed record GetProfessionLookupsQuery : IRequest<IReadOnlyList<LookupDto>>, IAnyAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.ProfessionManage, AppPermission.EvaluationCreate];
}
public sealed record SaveEmployeeCommand(int? Id, string FullName, int ProfessionId, bool IsActive) : IRequest<EmployeeDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EmployeeManage;
}

public sealed record GetProfessionsQuery : IRequest<IReadOnlyList<ProfessionDto>>, IAnyAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.ProfessionManage, AppPermission.KpiManage, AppPermission.EvaluationCreate, AppPermission.EvaluationHistory];
}
public sealed record SaveProfessionCommand(int? Id, string Name, bool IsActive) : IRequest<ProfessionDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.ProfessionManage;
}

public sealed record GetPeriodsQuery(bool ActiveOnly) : IRequest<IReadOnlyList<PeriodDto>>, IAnyAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.PeriodManage, AppPermission.EvaluationCreate, AppPermission.EvaluationHistory, AppPermission.DashboardView, AppPermission.GoalManage];
}
public sealed record SavePeriodCommand(int? Id, string Name, DateOnly StartDate, DateOnly EndDate, bool IsActive) : IRequest<PeriodDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.PeriodManage;
}

public sealed record GetGoalsQuery : IRequest<IReadOnlyList<CompanyGoalDto>>, IAnyAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.GoalManage, AppPermission.KpiManage, AppPermission.DashboardView];
}
public sealed record SaveGoalCommand(int? Id, string Name, string? Description, int? EvaluationPeriodId, int Priority, bool IsActive)
    : IRequest<CompanyGoalDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.GoalManage;
}

public sealed record GetKpisQuery : IRequest<IReadOnlyList<KpiDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.KpiManage;
}
public sealed record GetKpisForProfessionQuery(int ProfessionId, int? EvaluationPeriodId) : IRequest<IReadOnlyList<KpiDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationCreate;
}
public sealed record SaveKpiCommand(
    int? Id,
    string Title,
    string? Description,
    int? ProfessionId,
    bool IsCommon,
    int DisplayOrder,
    bool IsActive,
    string? Category,
    int? CompanyGoalId,
    KpiMeasurementType MeasurementType,
    string? Unit,
    decimal? TargetValue,
    KpiTargetDirection TargetDirection,
    decimal WeightPercent,
    string? DataSource,
    KpiReviewFrequency ReviewFrequency,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool IsSpecific,
    bool IsMeasurable,
    bool IsAchievable,
    bool IsRelevant,
    bool IsTimeBound) : IRequest<KpiDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.KpiManage;
}

public sealed record GetEvaluationHistoryQuery(int? EmployeeId, int? ProfessionId, int? PeriodId, int? EvaluatorUserId = null)
    : IRequest<IReadOnlyList<EvaluationSummaryDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationHistory;
}
public sealed record GetEvaluationDetailsQuery(long Id) : IRequest<IReadOnlyList<EvaluationDetailDto>>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationHistory;
}
public sealed record GetEvaluationForEditQuery(long Id) : IRequest<EvaluationEditDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationCreate;
}
public sealed record SaveEvaluationCommand(EvaluationSaveInput Input) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationCreate;
}
public sealed record UpdateEvaluationCommand(
    long Id,
    string? GeneralNote,
    IReadOnlyList<EvaluationItemUpdateInput> Items,
    EvaluationStatus Status,
    string? ManagerFeedback,
    string? EmployeeFeedback,
    string? DevelopmentAction,
    DateOnly? FollowUpDate) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationCreate;
}
public sealed record ApproveEvaluationCommand(long Id) : IRequest, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationApprove;
}
