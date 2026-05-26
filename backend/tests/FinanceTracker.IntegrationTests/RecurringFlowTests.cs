using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Features.Auth;
using FinanceTracker.Application.Features.Categories;
using FinanceTracker.Application.Features.Recurring;
using FinanceTracker.Application.Features.Transactions;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace FinanceTracker.IntegrationTests;

public class RecurringFlowTests : IClassFixture<TestWebApp>
{
    private readonly TestWebApp _app;

    public RecurringFlowTests(TestWebApp app) { _app = app; }

    private async Task<HttpClient> RegisterClientAsync()
    {
        var http = _app.CreateClient();
        var email = $"u{Guid.NewGuid():N}@example.com";
        var resp = await http.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Password1!", "Test User", "INR"));
        resp.EnsureSuccessStatusCode();
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return http;
    }

    private static async Task<Guid> ExpenseCategoryAsync(HttpClient http)
    {
        var cats = (await http.GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories"))!;
        return cats.First(c => c.Type == "Expense").Id;
    }

    [Fact]
    public async Task Create_List_Update_Delete_RecurringRule()
    {
        var http = await RegisterClientAsync();
        var catId = await ExpenseCategoryAsync(http);

        var create = await http.PostAsJsonAsync("/api/v1/recurring",
            new CreateRecurringRequest(catId, null, 19.99m, "Expense", "Netflix", "Monthly",
                DateTimeOffset.UtcNow.AddDays(3), null));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var rule = (await create.Content.ReadFromJsonAsync<RecurringDto>())!;
        rule.Frequency.Should().Be("Monthly");
        rule.IsActive.Should().BeTrue();
        rule.NextRunDate.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(3), TimeSpan.FromMinutes(1));

        var list = (await http.GetFromJsonAsync<List<RecurringDto>>("/api/v1/recurring"))!;
        list.Should().ContainSingle();

        var update = await http.PutAsJsonAsync($"/api/v1/recurring/{rule.Id}",
            new UpdateRecurringRequest(catId, null, 25m, "Expense", "Netflix Premium", "Monthly", null, false));
        update.IsSuccessStatusCode.Should().BeTrue();
        var updated = (await update.Content.ReadFromJsonAsync<RecurringDto>())!;
        updated.Amount.Should().Be(25m);
        updated.IsActive.Should().BeFalse();

        var del = await http.DeleteAsync($"/api/v1/recurring/{rule.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await http.GetFromJsonAsync<List<RecurringDto>>("/api/v1/recurring"))!.Should().BeEmpty();
    }

    [Fact]
    public async Task RunNow_CreatesTransaction_AndAdvancesNextRun()
    {
        var http = await RegisterClientAsync();
        var catId = await ExpenseCategoryAsync(http);

        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var create = await http.PostAsJsonAsync("/api/v1/recurring",
            new CreateRecurringRequest(catId, null, 50m, "Expense", "Gym", "Weekly", start, null));
        var rule = (await create.Content.ReadFromJsonAsync<RecurringDto>())!;

        var run = await http.PostAsync($"/api/v1/recurring/{rule.Id}/run", null);
        run.IsSuccessStatusCode.Should().BeTrue();
        var after = (await run.Content.ReadFromJsonAsync<RecurringDto>())!;
        after.LastRunAt.Should().NotBeNull();
        after.NextRunDate.Should().BeCloseTo(start.AddDays(7), TimeSpan.FromMinutes(1));

        var txs = (await http.GetFromJsonAsync<TransactionListResponse>("/api/v1/transactions"))!;
        txs.Total.Should().Be(1);
        txs.Items[0].Amount.Should().Be(50m);
    }

    [Fact]
    public async Task Materializer_CatchesUp_MissedOccurrences_AndStopsAtEndDate()
    {
        var http = await RegisterClientAsync();
        var catId = await ExpenseCategoryAsync(http);

        // Daily rule: start -4d, end -2d → occurrences at -4d, -3d, -2d (3 total), then deactivates.
        var now = DateTimeOffset.UtcNow;
        var start = now.AddDays(-4);
        var end = now.AddDays(-2);
        var create = await http.PostAsJsonAsync("/api/v1/recurring",
            new CreateRecurringRequest(catId, null, 5m, "Expense", "Coffee", "Daily", start, end));
        var rule = (await create.Content.ReadFromJsonAsync<RecurringDto>())!;

        // Drive the materializer directly (worker is disabled in tests).
        using (var scope = _app.Services.CreateScope())
        {
            var materializer = scope.ServiceProvider.GetRequiredService<IRecurringMaterializer>();
            var created = await materializer.RunAsync(DateTimeOffset.UtcNow);
            created.Should().Be(3);
        }

        var txs = (await http.GetFromJsonAsync<TransactionListResponse>("/api/v1/transactions"))!;
        txs.Total.Should().Be(3);

        // Rule should now be inactive (past its end date).
        var list = (await http.GetFromJsonAsync<List<RecurringDto>>("/api/v1/recurring"))!;
        list.Single(r => r.Id == rule.Id).IsActive.Should().BeFalse();
    }
}
