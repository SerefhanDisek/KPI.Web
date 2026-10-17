using System.Security.Claims;
using KpiEvaluation.Application;
using KpiEvaluation.Application.Features;
using KpiEvaluation.Domain;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticatedUserDto>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await sender.Send(command, cancellationToken);
        if (user is null) return Unauthorized(new { detail = "Kullanıcı adı veya parola hatalı." });
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.RoleCode)
        };
        if (user.EmployeeId is not null) claims.Add(new("employee_id", user.EmployeeId.Value.ToString()));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });
        return Ok(user);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    public Task<AuthenticatedUserDto> Me(CancellationToken cancellationToken) => sender.Send(new GetCurrentUserQuery(), cancellationToken);

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}

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

[ApiController]
[Route("api/pages")]
public sealed class PageDefinitionsController(ISender sender) : ControllerBase
{
    [HttpGet("accessible")]
    public Task<IReadOnlyList<PageDefinitionDto>> GetAccessible(CancellationToken cancellationToken) => sender.Send(new GetAccessiblePagesQuery(), cancellationToken);

    [HttpGet]
    public Task<IReadOnlyList<PageDefinitionDto>> GetAll(CancellationToken cancellationToken) => sender.Send(new GetPageDefinitionsQuery(), cancellationToken);

    [HttpPost]
    public Task<PageDefinitionDto> Save(SavePageDefinitionCommand command, CancellationToken cancellationToken) => sender.Send(command, cancellationToken);
}
