using KpiEvaluation.Application;
using KpiEvaluation.Infrastructure;
using KPI.Web.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".data-protection")))
    .ProtectKeysWithDpapi()
    .SetApplicationName("KpiEvaluation.Web");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "kpi.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();
var authenticatedPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
builder.Services.AddControllers(options => options.Filters.Add(new AuthorizeFilter(authenticatedPolicy)));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("KpiDatabase")
    ?? throw new InvalidOperationException("KpiDatabase bağlantı bilgisi bulunamadı."),
    builder.Configuration["Authentication:BootstrapPassword"]);

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

await DatabaseStartup.TryInitializeAsync(app.Services, app.Logger);
await app.RunAsync();
