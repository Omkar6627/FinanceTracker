using System.Text.Json;
using FinanceTracker.Domain.Common;

namespace FinanceTracker.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _log;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (DomainException dex)
        {
            _log.LogWarning(dex, "Domain rule violation");
            await WriteAsync(ctx, StatusCodes.Status400BadRequest, dex.Message);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled exception");
            await WriteAsync(ctx, StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }

    private static async Task WriteAsync(HttpContext ctx, int status, string message)
    {
        if (ctx.Response.HasStarted) return;
        ctx.Response.Clear();
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { message }));
    }
}
