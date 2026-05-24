using FinanceTracker.Application.Common;
using FinanceTracker.Application.Features.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Endpoints;

public static class AuthEndpoints
{
    private const string Base = "/api/v1/auth";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost($"{Base}/register", async ([FromBody] RegisterRequest req, IAuthService svc, CancellationToken ct) =>
            (await svc.RegisterAsync(req, ct)).ToHttpResult(StatusCodes.Status201Created))
            .WithTags("Auth").AllowAnonymous();

        app.MapPost($"{Base}/login", async ([FromBody] LoginRequest req, IAuthService svc, CancellationToken ct) =>
            (await svc.LoginAsync(req, ct)).ToHttpResult())
            .WithTags("Auth").AllowAnonymous();

        app.MapPost($"{Base}/refresh", async ([FromBody] RefreshRequest req, IAuthService svc, CancellationToken ct) =>
            (await svc.RefreshAsync(req, ct)).ToHttpResult())
            .WithTags("Auth").AllowAnonymous();

        app.MapGet($"{Base}/me", async (IAuthService svc, ICurrentUser current, CancellationToken ct) =>
        {
            if (current.UserId is null) return Results.Unauthorized();
            return (await svc.GetProfileAsync(current.UserId.Value, ct)).ToHttpResult();
        }).WithTags("Auth").RequireAuthorization();

        app.MapPost($"{Base}/invite/accept", async ([FromBody] AcceptInvitationRequest req, IAuthService svc, CancellationToken ct) =>
            (await svc.AcceptInvitationAsync(req, ct)).ToHttpResult(StatusCodes.Status201Created))
            .WithTags("Auth").AllowAnonymous();

        return app;
    }
}
