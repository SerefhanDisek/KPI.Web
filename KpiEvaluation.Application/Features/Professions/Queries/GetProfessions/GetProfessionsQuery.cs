using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Professions.Queries.GetProfessions;

public sealed record GetProfessionsQuery : IRequest<IReadOnlyList<ProfessionDto>>, IAnyAuthorizedRequest
{
    public IReadOnlyCollection<AppPermission> Permissions => [AppPermission.ProfessionManage, AppPermission.KpiManage, AppPermission.EvaluationCreate, AppPermission.EvaluationHistory, AppPermission.LeaveView];
}
