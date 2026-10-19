using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Development.Commands.SaveDevelopmentItem;

public sealed class SaveDevelopmentItemCommandHandler(IDevelopmentRepository repository, ICurrentUser currentUser) : IRequestHandler<SaveDevelopmentItemCommand, DevelopmentItemDto>
{
    public Task<DevelopmentItemDto> Handle(SaveDevelopmentItemCommand request, CancellationToken cancellationToken) =>
        repository.SaveItemAsync(request.Input, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}

