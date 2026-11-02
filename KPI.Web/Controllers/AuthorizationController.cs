using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Authorization.Commands.DeactivateRole;
using KpiEvaluation.Application.Features.Authorization.Commands.DeactivateUser;
using KpiEvaluation.Application.Features.Authorization.Commands.DeleteRole;
using KpiEvaluation.Application.Features.Authorization.Commands.DeleteUser;
using KpiEvaluation.Application.Features.Authorization.Commands.SaveRole;
using KpiEvaluation.Application.Features.Authorization.Commands.SaveRolePermissions;
using KpiEvaluation.Application.Features.Authorization.Commands.SaveUser;
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

    [HttpPost("users/{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateUser(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeactivateUserCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteUserCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("roles/{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateRole(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeactivateRoleCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("roles/{id:int}")]
    public async Task<IActionResult> DeleteRole(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteRoleCommand(id), cancellationToken);
        return NoContent();
    }
}
