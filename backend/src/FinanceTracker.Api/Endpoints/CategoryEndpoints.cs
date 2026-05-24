using FinanceTracker.Application.Features.Categories;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Endpoints;

public static class CategoryEndpoints
{
    private const string Base = "/api/v1/categories";

    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Base, async (ICategoryService svc, CancellationToken ct) =>
            (await svc.ListAsync(ct)).ToHttpResult())
            .WithTags("Categories").RequireAuthorization();

        app.MapPost(Base, async ([FromBody] CreateCategoryRequest req, ICategoryService svc, CancellationToken ct) =>
            (await svc.CreateAsync(req, ct)).ToHttpResult(StatusCodes.Status201Created))
            .WithTags("Categories").RequireAuthorization();

        app.MapPut($"{Base}/{{id:guid}}", async (Guid id, [FromBody] UpdateCategoryRequest req, ICategoryService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(id, req, ct)).ToHttpResult())
            .WithTags("Categories").RequireAuthorization();

        app.MapDelete($"{Base}/{{id:guid}}", async (Guid id, ICategoryService svc, CancellationToken ct) =>
            (await svc.DeleteAsync(id, ct)).ToHttpResult())
            .WithTags("Categories").RequireAuthorization();

        return app;
    }
}
