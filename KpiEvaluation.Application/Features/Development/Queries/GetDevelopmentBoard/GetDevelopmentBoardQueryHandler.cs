using KpiEvaluation.Application.Repositories;
using MediatR;

namespace KpiEvaluation.Application.Features.Development.Queries.GetDevelopmentBoard;

public sealed class GetDevelopmentBoardQueryHandler(IDevelopmentRepository repository, ICurrentUser currentUser) : IRequestHandler<GetDevelopmentBoardQuery, DevelopmentBoardDto>
{
    public Task<DevelopmentBoardDto> Handle(GetDevelopmentBoardQuery request, CancellationToken cancellationToken) =>
        repository.GetBoardAsync(request.EmployeeId, request.ActiveOnly, request.Page, request.PageSize, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}

