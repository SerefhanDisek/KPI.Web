using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Workflow.Commands.RespondToEvaluation;

public sealed class RespondToEvaluationCommandHandler(IEvaluationWorkflowRepository repository, ICurrentUser currentUser) : IRequestHandler<RespondToEvaluationCommand>
{
    public Task Handle(RespondToEvaluationCommand request, CancellationToken cancellationToken) =>
        repository.RespondAsync(request.EvaluationId, request.Decision, request.Comment, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}

