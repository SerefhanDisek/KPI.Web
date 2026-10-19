using System.Security.Claims;
using KpiEvaluation.Application;
using KpiEvaluation.Application.Features.Authentication.Commands.ChangePassword;
using KpiEvaluation.Application.Features.Authentication.Commands.Login;
using KpiEvaluation.Application.Features.Authentication.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;

namespace KPI.Web.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("login-defaults")]
    public IActionResult LoginDefaults([FromServices] IWebHostEnvironment environment, [FromServices] IConfiguration configuration)
    {
        Response.Headers.CacheControl = "no-store";
        var password = configuration["Authentication:BootstrapPassword"];
        if (!environment.IsDevelopment() || string.IsNullOrWhiteSpace(password)) return NoContent();
        return Ok(new { userName = "admin", password });
    }

    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult Csrf([FromServices] IAntiforgery antiforgery)
    {
        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers.Pragma = "no-cache";
        return Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

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
