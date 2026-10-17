using MediatR;

namespace KpiEvaluation.Application.Features;

public sealed class GetDashboardHandler(IDashboardService service, ICurrentUser currentUser) : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.PeriodId, cancellationToken, currentUser.UserId, request.EvaluatorUserId);
}

public sealed class GetEmployeesHandler(IEmployeeService service) : IRequestHandler<GetEmployeesQuery, IReadOnlyList<EmployeeDto>>
{
    public Task<IReadOnlyList<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken) =>
        service.GetAllAsync(request.ActiveOnly, cancellationToken);
}

public sealed class GetProfessionLookupsHandler(IEmployeeService service) : IRequestHandler<GetProfessionLookupsQuery, IReadOnlyList<LookupDto>>
{
    public Task<IReadOnlyList<LookupDto>> Handle(GetProfessionLookupsQuery request, CancellationToken cancellationToken) =>
        service.GetProfessionsAsync(cancellationToken);
}

public sealed class SaveEmployeeHandler(IEmployeeService service) : IRequestHandler<SaveEmployeeCommand, EmployeeDto>
{
    public Task<EmployeeDto> Handle(SaveEmployeeCommand request, CancellationToken cancellationToken) =>
        service.SaveAsync(request.Id, request.FullName, request.ProfessionId, request.IsActive, cancellationToken);
}

public sealed class GetProfessionsHandler(IProfessionService service) : IRequestHandler<GetProfessionsQuery, IReadOnlyList<ProfessionDto>>
{
    public Task<IReadOnlyList<ProfessionDto>> Handle(GetProfessionsQuery request, CancellationToken cancellationToken) =>
        service.GetAllAsync(cancellationToken);
}

public sealed class SaveProfessionHandler(IProfessionService service) : IRequestHandler<SaveProfessionCommand, ProfessionDto>
{
    public Task<ProfessionDto> Handle(SaveProfessionCommand request, CancellationToken cancellationToken) =>
        service.SaveAsync(request.Id, request.Name, request.IsActive, cancellationToken);
}

public sealed class GetPeriodsHandler(IEvaluationPeriodService service) : IRequestHandler<GetPeriodsQuery, IReadOnlyList<PeriodDto>>
{
    public Task<IReadOnlyList<PeriodDto>> Handle(GetPeriodsQuery request, CancellationToken cancellationToken) =>
        service.GetAllAsync(request.ActiveOnly, cancellationToken);
}

public sealed class SavePeriodHandler(IEvaluationPeriodService service) : IRequestHandler<SavePeriodCommand, PeriodDto>
{
    public Task<PeriodDto> Handle(SavePeriodCommand request, CancellationToken cancellationToken) =>
        service.SaveAsync(request.Id, request.Name, request.StartDate, request.EndDate, request.IsActive, cancellationToken);
}

public sealed class GetGoalsHandler(ICompanyGoalService service) : IRequestHandler<GetGoalsQuery, IReadOnlyList<CompanyGoalDto>>
{
    public Task<IReadOnlyList<CompanyGoalDto>> Handle(GetGoalsQuery request, CancellationToken cancellationToken) =>
        service.GetAllAsync(cancellationToken);
}

public sealed class SaveGoalHandler(ICompanyGoalService service) : IRequestHandler<SaveGoalCommand, CompanyGoalDto>
{
    public Task<CompanyGoalDto> Handle(SaveGoalCommand request, CancellationToken cancellationToken) =>
        service.SaveAsync(request.Id, request.Name, request.Description, request.EvaluationPeriodId, request.Priority,
            request.IsActive, cancellationToken);
}

public sealed class GetKpisHandler(IKpiService service) : IRequestHandler<GetKpisQuery, IReadOnlyList<KpiDto>>
{
    public Task<IReadOnlyList<KpiDto>> Handle(GetKpisQuery request, CancellationToken cancellationToken) =>
        service.GetAllAsync(cancellationToken);
}

public sealed class GetKpisForProfessionHandler(IKpiService service) : IRequestHandler<GetKpisForProfessionQuery, IReadOnlyList<KpiDto>>
{
    public Task<IReadOnlyList<KpiDto>> Handle(GetKpisForProfessionQuery request, CancellationToken cancellationToken) =>
        service.GetForProfessionAsync(request.ProfessionId, request.EvaluationPeriodId, cancellationToken);
}

public sealed class SaveKpiHandler(IKpiService service) : IRequestHandler<SaveKpiCommand, KpiDto>
{
    public Task<KpiDto> Handle(SaveKpiCommand request, CancellationToken cancellationToken) =>
        service.SaveAsync(request.Id, request.Title, request.Description, request.ProfessionId, request.IsCommon,
            request.DisplayOrder, request.IsActive, cancellationToken, request.Category, request.CompanyGoalId,
            request.MeasurementType, request.Unit, request.TargetValue, request.TargetDirection, request.WeightPercent,
            request.DataSource, request.ReviewFrequency, request.ValidFrom, request.ValidTo, request.IsSpecific,
            request.IsMeasurable, request.IsAchievable, request.IsRelevant, request.IsTimeBound);
}

public sealed class GetEvaluationHistoryHandler(IEvaluationService service, ICurrentUser currentUser)
    : IRequestHandler<GetEvaluationHistoryQuery, IReadOnlyList<EvaluationSummaryDto>>
{
    public Task<IReadOnlyList<EvaluationSummaryDto>> Handle(GetEvaluationHistoryQuery request, CancellationToken cancellationToken) =>
        service.GetHistoryAsync(request.EmployeeId, request.ProfessionId, request.PeriodId, cancellationToken, currentUser.UserId, request.EvaluatorUserId);
}

public sealed class GetEvaluationDetailsHandler(IEvaluationService service, ICurrentUser currentUser)
    : IRequestHandler<GetEvaluationDetailsQuery, IReadOnlyList<EvaluationDetailDto>>
{
    public Task<IReadOnlyList<EvaluationDetailDto>> Handle(GetEvaluationDetailsQuery request, CancellationToken cancellationToken) =>
        service.GetDetailsAsync(request.Id, cancellationToken, currentUser.UserId);
}

public sealed class GetEvaluationForEditHandler(IEvaluationService service, ICurrentUser currentUser) : IRequestHandler<GetEvaluationForEditQuery, EvaluationEditDto>
{
    public Task<EvaluationEditDto> Handle(GetEvaluationForEditQuery request, CancellationToken cancellationToken) =>
        service.GetForEditAsync(request.Id, cancellationToken, currentUser.UserId);
}

public sealed class SaveEvaluationHandler(IEvaluationService service, ICurrentUser currentUser) : IRequestHandler<SaveEvaluationCommand>
{
    public Task Handle(SaveEvaluationCommand request, CancellationToken cancellationToken) =>
        service.SaveAsync(request.Input, cancellationToken, currentUser.UserId);
}

public sealed class UpdateEvaluationHandler(IEvaluationService service, ICurrentUser currentUser) : IRequestHandler<UpdateEvaluationCommand>
{
    public Task Handle(UpdateEvaluationCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.Id, request.GeneralNote, request.Items, cancellationToken, request.Status,
            request.ManagerFeedback, request.EmployeeFeedback, request.DevelopmentAction, request.FollowUpDate, currentUser.UserId);
}

public sealed class ApproveEvaluationHandler(IEvaluationService service, ICurrentUser currentUser) : IRequestHandler<ApproveEvaluationCommand>
{
    public Task Handle(ApproveEvaluationCommand request, CancellationToken cancellationToken) =>
        service.ApproveAsync(request.Id, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}
