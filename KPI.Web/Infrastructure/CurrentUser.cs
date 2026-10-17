using System.Security.Claims;
using KpiEvaluation.Application;

namespace KPI.Web.Infrastructure;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public int? UserId => int.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
