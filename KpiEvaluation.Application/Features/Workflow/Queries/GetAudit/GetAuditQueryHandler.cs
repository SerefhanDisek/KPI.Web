using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Workflow.Queries.GetAudit;

public sealed class GetAuditQueryHandler(IEvaluationWorkflowRepository repository) : IRequestHandler<GetAuditQuery, AuditPageDto>
{
    public Task<AuditPageDto> Handle(GetAuditQuery request, CancellationToken cancellationToken) =>
        repository.GetAuditAsync(request.Page, request.PageSize, request.EntityType, cancellationToken);
}

