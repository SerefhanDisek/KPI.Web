using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Evaluations.Commands.UpdateEvaluation;

public sealed class UpdateEvaluationCommandHandler(IEvaluationRepository repository, ICurrentUser currentUser) : IRequestHandler<UpdateEvaluationCommand>
{
    public Task Handle(UpdateEvaluationCommand request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(request.Id, request.GeneralNote, request.Items, cancellationToken, request.Status,
            request.ManagerFeedback, request.EmployeeFeedback, request.DevelopmentAction, request.FollowUpDate, currentUser.UserId);
}
