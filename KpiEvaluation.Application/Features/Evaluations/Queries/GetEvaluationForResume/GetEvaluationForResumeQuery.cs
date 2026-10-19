using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Queries.GetEvaluationForResume;

public sealed record GetEvaluationForResumeQuery(int EmployeeId, int PeriodId) : IRequest<EvaluationResumeDto?>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.EvaluationCreate;
}
