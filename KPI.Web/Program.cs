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
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();
var authenticatedPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new AuthorizeFilter(authenticatedPolicy));
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("KpiDatabase"),
    builder.Configuration["Authentication:BootstrapPassword"], builder.Environment.IsDevelopment());
builder.Services.AddSingleton<DatabaseStartup>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<DatabaseStartup>());

var app = builder.Build();

app.UseMiddleware<BrowserSecurityMiddleware>();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseDefaultFiles();
var staticFiles = new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (context.File.Name.Equals("index.html", StringComparison.OrdinalIgnoreCase))
            context.Context.Response.Headers.CacheControl = "no-cache";
    }
};
app.UseStaticFiles(staticFiles);
app.UseAuthentication();
app.UseMiddleware<AntiforgeryValidationMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapFallback("/api/{**path}", context => { context.Response.StatusCode = StatusCodes.Status404NotFound; return Task.CompletedTask; });
app.MapFallbackToFile("index.html", staticFiles);

await app.RunAsync();
