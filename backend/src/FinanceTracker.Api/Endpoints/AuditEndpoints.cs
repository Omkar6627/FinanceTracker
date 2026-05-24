using FinanceTracker.Application.Features.Audit;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Endpoints;

public static class AuditEndpoints
{
    private const string Base = "/api/v1/audit";

    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Base, async (
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] Guid? actorId,
            [FromQuery] string? action,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IAuditQueryService svc,
            CancellationToken ct) =>
        {
            var q = new AuditQuery(from, to, actorId, action, page ?? 1, pageSize ?? 50);
            return (await svc.ListAsync(q, ct)).ToHttpResult();
        }).WithTags("Audit").RequireAuthorization();

        return app;
    }
}
