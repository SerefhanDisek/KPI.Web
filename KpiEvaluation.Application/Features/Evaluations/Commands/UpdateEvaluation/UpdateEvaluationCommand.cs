using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Commands.UpdateEvaluation;

public sealed record UpdateEvaluationCommand(
    long Id,
    string? GeneralNote,
    IReadOnlyList<EvaluationItemUpdateInput> Items,
    EvaluationStatus Status,
    string? ManagerFeedback,
    string? EmployeeFeedback,
    string? DevelopmentAction,
    DateOnly? FollowUpDate) : IRequest, IAllAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => Status == EvaluationStatus.Draft
        ? [AppPermission.EvaluationCreate] : [AppPermission.EvaluationCreate, AppPermission.EvaluationApprove];
}
