using MediatR;

namespace KpiEvaluation.Application.Features;

public sealed class GetEmployeeLeavesHandler(IEmployeeLeaveService service) : IRequestHandler<GetEmployeeLeavesQuery, EmployeeLeavePageDto>
{
    public Task<EmployeeLeavePageDto> Handle(GetEmployeeLeavesQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.EmployeeId, request.ProfessionId, request.From, request.To,
            request.ActiveOnly, request.Page, request.PageSize, cancellationToken);
}

public sealed class SaveEmployeeLeaveHandler(IEmployeeLeaveService service, ICurrentUser currentUser)
    : IRequestHandler<SaveEmployeeLeaveCommand, EmployeeLeaveDto>
{
    public Task<EmployeeLeaveDto> Handle(SaveEmployeeLeaveCommand request, CancellationToken cancellationToken) =>
        service.SaveAsync(request.Input, currentUser.UserId ?? throw new AuthorizationException(), cancellationToken);
}
