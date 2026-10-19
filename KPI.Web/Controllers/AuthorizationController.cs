using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Authorization.Commands.SaveEmployeeManagers;
using KpiEvaluation.Application.Features.Authorization.Commands.SaveRole;
using KpiEvaluation.Application.Features.Authorization.Commands.SaveRolePermissions;
using KpiEvaluation.Application.Features.Authorization.Commands.SaveUser;
using KpiEvaluation.Application.Features.Authorization.Queries.GetEmployeeManagers;
using KpiEvaluation.Application.Features.Authorization.Queries.GetRolePermissions;
using KpiEvaluation.Application.Features.Authorization.Queries.GetRoles;
using KpiEvaluation.Application.Features.Authorization.Queries.GetUsers;
using KpiEvaluation.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/authorization")]
public sealed class AuthorizationController(ISender sender) : ControllerBase
{
    [HttpGet("users")]
    public Task<IReadOnlyList<UserDto>> GetUsers(CancellationToken cancellationToken) => sender.Send(new GetUsersQuery(), cancellationToken);

    [HttpPost("users")]
    public Task<UserDto> SaveUser(UserSaveInput input, CancellationToken cancellationToken) => sender.Send(new SaveUserCommand(input), cancellationToken);

    [HttpGet("roles")]
    public Task<IReadOnlyList<RoleDto>> GetRoles(CancellationToken cancellationToken) => sender.Send(new GetRolesQuery(), cancellationToken);

    [HttpPost("roles")]
    public Task<RoleDto> SaveRole(SaveRoleCommand command, CancellationToken cancellationToken) => sender.Send(command, cancellationToken);

    [HttpGet("permissions")]
    public Task<IReadOnlyList<RolePermissionDto>> GetPermissions(CancellationToken cancellationToken) => sender.Send(new GetRolePermissionsQuery(), cancellationToken);

    [HttpPut("permissions/{roleId:int}")]
    public async Task<IActionResult> SavePermissions(int roleId, IReadOnlyList<AppPermission> permissions, CancellationToken cancellationToken)
    {
        await sender.Send(new SaveRolePermissionsCommand(roleId, permissions), cancellationToken);
        return NoContent();
    }

    [HttpGet("employee-managers")]
    public Task<IReadOnlyList<EmployeeManagerDto>> GetManagers(CancellationToken cancellationToken) => sender.Send(new GetEmployeeManagersQuery(), cancellationToken);

    [HttpPut("employee-managers/{employeeId:int}")]
    public async Task<IActionResult> SaveManagers(int employeeId, SaveEmployeeManagersCommand command, CancellationToken cancellationToken)
    {
        if (employeeId != command.EmployeeId) return BadRequest();
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
