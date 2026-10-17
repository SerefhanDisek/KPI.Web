using KpiEvaluation.Application;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Infrastructure;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (exception is ValidationException or DuplicateEvaluationException)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "İşlem tamamlanamadı", exception.Message);
        }
        catch (AuthorizationException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "Yetkisiz işlem", exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "İstek işlenirken beklenmeyen bir hata oluştu.");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Beklenmeyen hata",
                "İşlem tamamlanamadı. Veritabanı bağlantısını kontrol edip tekrar deneyin.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        });
    }
}
