using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceTracker.Application.Features.Auth;
using FinanceTracker.Application.Features.Categories;
using FinanceTracker.Application.Features.Members;
using FinanceTracker.Application.Features.Organisations;
using FinanceTracker.Application.Features.Transactions;
using FluentAssertions;
using Xunit;

namespace FinanceTracker.IntegrationTests;

public class EnterpriseFlowTests : IClassFixture<TestWebApp>
{
    private readonly TestWebApp _app;

    public EnterpriseFlowTests(TestWebApp app) { _app = app; }

    private async Task<(HttpClient http, AuthResponse auth)> RegisterAsync(string emailSeed)
    {
        var http = _app.CreateClient();
        var email = $"e{emailSeed}{Guid.NewGuid():N}@test.com";
        var resp = await http.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Password1!", $"User {emailSeed}", "USD"));
        resp.EnsureSuccessStatusCode();
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (http, auth);
    }

    private static HttpClient Authed(HttpClient http, string accessToken)
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return http;
    }

    [Fact]
    public async Task Owner_CanSwitch_To_Enterprise()
    {
        var (http, _) = await RegisterAsync("owner");

        var switchResp = await http.PutAsJsonAsync("/api/v1/organisation/mode", new SwitchModeRequest("Enterprise"));
        switchResp.IsSuccessStatusCode.Should().BeTrue();
        var org = (await switchResp.Content.ReadFromJsonAsync<OrganisationDto>())!;
        org.Mode.Should().Be("Enterprise");
    }

    [Fact]
    public async Task Enterprise_Invite_Accept_Submit_Approve_FullFlow()
    {
        var (ownerHttp, _) = await RegisterAsync("owner");

        // Switch to Enterprise. The owner's JWT still says OrganisationMode=Individual,
        // but the role claim ("Owner") is what permission checks use — that's unchanged,
        // so member-invite + approve still work without re-issuing the token.
        var sw = await ownerHttp.PutAsJsonAsync("/api/v1/organisation/mode", new SwitchModeRequest("Enterprise"));
        sw.EnsureSuccessStatusCode();

        // Owner invites a Member
        var inviteEmail = $"invitee{Guid.NewGuid():N}@test.com";
        var inviteResp = await ownerHttp.PostAsJsonAsync("/api/v1/members/invite",
            new InviteMemberRequest(inviteEmail, "Member", null));
        inviteResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var inv = (await inviteResp.Content.ReadFromJsonAsync<InvitationDto>())!;
        inv.Token.Should().NotBeNullOrWhiteSpace();

        // Invitee accepts (uses token to create user + join the org)
        var inviteeHttp = _app.CreateClient();
        var accept = await inviteeHttp.PostAsJsonAsync("/api/v1/auth/invite/accept",
            new AcceptInvitationRequest(inv.Token, "Invitee User", "Password1!"));
        accept.StatusCode.Should().Be(HttpStatusCode.Created);
        var inviteeAuth = (await accept.Content.ReadFromJsonAsync<AuthResponse>())!;
        inviteeAuth.User.Role.Should().Be("Member");
        Authed(inviteeHttp, inviteeAuth.AccessToken);

        // Invitee submits an Expense transaction (Enterprise → starts PendingApproval)
        var cats = (await inviteeHttp.GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories"))!;
        var expense = cats.First(c => c.Type == "Expense");
        var txResp = await inviteeHttp.PostAsJsonAsync("/api/v1/transactions",
            new CreateTransactionRequest(expense.Id, null, 250m, "Expense", "Client lunch", DateTimeOffset.UtcNow));
        txResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var tx = (await txResp.Content.ReadFromJsonAsync<TransactionDto>())!;
        tx.Status.Should().Be("PendingApproval");

        // Owner sees it in the pending queue
        var pending = await ownerHttp.GetFromJsonAsync<TransactionListResponse>("/api/v1/transactions/pending");
        pending!.Total.Should().BeGreaterThanOrEqualTo(1);
        pending.Items.Should().Contain(i => i.Id == tx.Id);

        // Owner approves
        var approve = await ownerHttp.PostAsync($"/api/v1/transactions/{tx.Id}/approve", null);
        approve.IsSuccessStatusCode.Should().BeTrue();
        var approved = (await approve.Content.ReadFromJsonAsync<TransactionDto>())!;
        approved.Status.Should().Be("Approved");

        // Invitee can't approve (Member role) — try and expect 403
        var inviteeApprove = await inviteeHttp.PostAsync($"/api/v1/transactions/{tx.Id}/approve", null);
        inviteeApprove.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NonOwner_CannotSwitchMode()
    {
        var (ownerHttp, _) = await RegisterAsync("owner2");
        await ownerHttp.PutAsJsonAsync("/api/v1/organisation/mode", new SwitchModeRequest("Enterprise"));

        var inviteEmail = $"admin{Guid.NewGuid():N}@test.com";
        var invResp = await ownerHttp.PostAsJsonAsync("/api/v1/members/invite",
            new InviteMemberRequest(inviteEmail, "Admin", null));
        var inv = (await invResp.Content.ReadFromJsonAsync<InvitationDto>())!;

        var adminHttp = _app.CreateClient();
        var accept = await adminHttp.PostAsJsonAsync("/api/v1/auth/invite/accept",
            new AcceptInvitationRequest(inv.Token, "Admin Person", "Password1!"));
        var adminAuth = (await accept.Content.ReadFromJsonAsync<AuthResponse>())!;
        Authed(adminHttp, adminAuth.AccessToken);

        // Admin tries to switch mode — Forbidden (only Owner can)
        var switchResp = await adminHttp.PutAsJsonAsync("/api/v1/organisation/mode", new SwitchModeRequest("Individual"));
        switchResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
