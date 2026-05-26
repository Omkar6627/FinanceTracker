using FinanceTracker.Application.Features.Recurring;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Endpoints;

public static class RecurringEndpoints
{
    private const string Base = "/api/v1/recurring";

    public static IEndpointRouteBuilder MapRecurringEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Base, async (IRecurringService svc, CancellationToken ct) =>
            (await svc.ListAsync(ct)).ToHttpResult())
            .WithTags("Recurring").RequireAuthorization();

        app.MapPost(Base, async ([FromBody] CreateRecurringRequest req, IRecurringService svc, CancellationToken ct) =>
            (await svc.CreateAsync(req, ct)).ToHttpResult(StatusCodes.Status201Created))
            .WithTags("Recurring").RequireAuthorization();

        app.MapPut($"{Base}/{{id:guid}}", async (Guid id, [FromBody] UpdateRecurringRequest req, IRecurringService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(id, req, ct)).ToHttpResult())
            .WithTags("Recurring").RequireAuthorization();

        app.MapDelete($"{Base}/{{id:guid}}", async (Guid id, IRecurringService svc, CancellationToken ct) =>
            (await svc.DeleteAsync(id, ct)).ToHttpResult())
            .WithTags("Recurring").RequireAuthorization();

        app.MapPost($"{Base}/{{id:guid}}/run", async (Guid id, IRecurringService svc, CancellationToken ct) =>
            (await svc.RunNowAsync(id, ct)).ToHttpResult())
            .WithTags("Recurring").RequireAuthorization();

        return app;
    }
}
