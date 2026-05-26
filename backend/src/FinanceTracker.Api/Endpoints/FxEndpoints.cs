using FinanceTracker.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Endpoints;

public static class FxEndpoints
{
    public static IEndpointRouteBuilder MapFxEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/fx/rates", async ([FromQuery] string? @base, IFxRateService fx, CancellationToken ct) =>
            Results.Ok(await fx.GetRatesAsync(string.IsNullOrWhiteSpace(@base) ? "USD" : @base, ct)))
            .WithTags("Fx").RequireAuthorization();

        return app;
    }
}
