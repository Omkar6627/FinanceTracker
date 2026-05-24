using FinanceTracker.Application.Features.Organisations;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Endpoints;

public static class OrganisationEndpoints
{
    private const string Base = "/api/v1/organisation";

    public static IEndpointRouteBuilder MapOrganisationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Base, async (IOrganisationService svc, CancellationToken ct) =>
            (await svc.GetAsync(ct)).ToHttpResult())
            .WithTags("Organisation").RequireAuthorization();

        app.MapPut(Base, async ([FromBody] UpdateOrganisationRequest req, IOrganisationService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(req, ct)).ToHttpResult())
            .WithTags("Organisation").RequireAuthorization();

        app.MapPut($"{Base}/mode", async ([FromBody] SwitchModeRequest req, IOrganisationService svc, CancellationToken ct) =>
            (await svc.SwitchModeAsync(req, ct)).ToHttpResult())
            .WithTags("Organisation").RequireAuthorization();

        return app;
    }
}
