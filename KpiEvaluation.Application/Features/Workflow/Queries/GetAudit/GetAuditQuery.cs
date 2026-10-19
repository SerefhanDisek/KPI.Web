using KpiEvaluation.Domain;
using MediatR;

namespace KpiEvaluation.Application.Features.Workflow.Queries.GetAudit;

public sealed record GetAuditQuery(int Page = 1, int PageSize = 50, string? EntityType = null) : IRequest<AuditPageDto>, IAuthorizedRequest
{
    public AppPermission Permission => AppPermission.AuditView;
}

