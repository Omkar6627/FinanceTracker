using FinanceTracker.Application.Features.Departments;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Endpoints;

public static class DepartmentEndpoints
{
    private const string Base = "/api/v1/departments";

    public static IEndpointRouteBuilder MapDepartmentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Base, async (IDepartmentService svc, CancellationToken ct) =>
            (await svc.ListAsync(ct)).ToHttpResult())
            .WithTags("Departments").RequireAuthorization();

        app.MapPost(Base, async ([FromBody] CreateDepartmentRequest req, IDepartmentService svc, CancellationToken ct) =>
            (await svc.CreateAsync(req, ct)).ToHttpResult(StatusCodes.Status201Created))
            .WithTags("Departments").RequireAuthorization();

        app.MapPut($"{Base}/{{id:guid}}", async (Guid id, [FromBody] UpdateDepartmentRequest req, IDepartmentService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(id, req, ct)).ToHttpResult())
            .WithTags("Departments").RequireAuthorization();

        app.MapDelete($"{Base}/{{id:guid}}", async (Guid id, IDepartmentService svc, CancellationToken ct) =>
            (await svc.DeleteAsync(id, ct)).ToHttpResult())
            .WithTags("Departments").RequireAuthorization();

        return app;
    }
}
