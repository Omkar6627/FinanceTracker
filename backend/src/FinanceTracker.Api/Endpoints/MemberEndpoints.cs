using FinanceTracker.Application.Features.Members;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Endpoints;

public static class MemberEndpoints
{
    private const string Base = "/api/v1/members";

    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Base, async (IMemberService svc, CancellationToken ct) =>
            (await svc.ListAsync(ct)).ToHttpResult())
            .WithTags("Members").RequireAuthorization();

        app.MapGet($"{Base}/invitations", async (IMemberService svc, CancellationToken ct) =>
            (await svc.ListInvitationsAsync(ct)).ToHttpResult())
            .WithTags("Members").RequireAuthorization();

        app.MapPost($"{Base}/invite", async ([FromBody] InviteMemberRequest req, IMemberService svc, CancellationToken ct) =>
            (await svc.InviteAsync(req, ct)).ToHttpResult(StatusCodes.Status201Created))
            .WithTags("Members").RequireAuthorization();

        app.MapDelete($"{Base}/invitations/{{id:guid}}", async (Guid id, IMemberService svc, CancellationToken ct) =>
            (await svc.RevokeInvitationAsync(id, ct)).ToHttpResult())
            .WithTags("Members").RequireAuthorization();

        app.MapPut($"{Base}/{{id:guid}}/role", async (Guid id, [FromBody] ChangeRoleRequest req, IMemberService svc, CancellationToken ct) =>
            (await svc.ChangeRoleAsync(id, req, ct)).ToHttpResult())
            .WithTags("Members").RequireAuthorization();

        app.MapDelete($"{Base}/{{id:guid}}", async (Guid id, IMemberService svc, CancellationToken ct) =>
            (await svc.RemoveAsync(id, ct)).ToHttpResult())
            .WithTags("Members").RequireAuthorization();

        return app;
    }
}
