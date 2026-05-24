using System.Net;
using System.Net.Http.Json;
using FinanceTracker.Application.Features.Auth;
using FluentAssertions;
using Xunit;

namespace FinanceTracker.IntegrationTests;

public class AuthFlowTests : IClassFixture<TestWebApp>
{
    private readonly HttpClient _http;

    public AuthFlowTests(TestWebApp app) { _http = app.CreateClient(); }

    [Fact]
    public async Task Register_New_ReturnsTokensAndProfile()
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest($"reg+{Guid.NewGuid():N}@example.com", "Password1!", "Test User", "INR"));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.User.OrganisationMode.Should().Be("Individual");
        body.User.Role.Should().Be("Owner");
    }

    [Fact]
    public async Task Register_Duplicate_ReturnsConflict()
    {
        var email = $"dup+{Guid.NewGuid():N}@example.com";
        var first = await _http.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Password1!", "Dup User", "INR"));
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var second = await _http.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Password1!", "Dup User", "INR"));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_BadCredentials_Returns401()
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("nobody@example.com", "wrong"));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var resp = await _http.GetAsync("/api/v1/auth/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest("weak@example.com", "short", "Test", "INR"));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
