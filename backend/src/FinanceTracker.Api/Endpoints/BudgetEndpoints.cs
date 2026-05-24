using FinanceTracker.Application.Features.Budgets;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Endpoints;

public static class BudgetEndpoints
{
    private const string Base = "/api/v1/budgets";

    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Base, async (IBudgetService svc, CancellationToken ct) =>
            (await svc.ListAsync(ct)).ToHttpResult())
            .WithTags("Budgets").RequireAuthorization();

        app.MapGet($"{Base}/status", async (IBudgetService svc, CancellationToken ct) =>
            (await svc.GetStatusAsync(ct)).ToHttpResult())
            .WithTags("Budgets").RequireAuthorization();

        app.MapPost(Base, async ([FromBody] CreateBudgetRequest req, IBudgetService svc, CancellationToken ct) =>
            (await svc.CreateAsync(req, ct)).ToHttpResult(StatusCodes.Status201Created))
            .WithTags("Budgets").RequireAuthorization();

        app.MapPut($"{Base}/{{id:guid}}", async (Guid id, [FromBody] UpdateBudgetRequest req, IBudgetService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(id, req, ct)).ToHttpResult())
            .WithTags("Budgets").RequireAuthorization();

        app.MapDelete($"{Base}/{{id:guid}}", async (Guid id, IBudgetService svc, CancellationToken ct) =>
            (await svc.DeleteAsync(id, ct)).ToHttpResult())
            .WithTags("Budgets").RequireAuthorization();

        return app;
    }
}
