using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceTracker.Application.Features.Auth;
using FinanceTracker.Application.Features.Budgets;
using FinanceTracker.Application.Features.Categories;
using FinanceTracker.Application.Features.Reports;
using FinanceTracker.Application.Features.Transactions;
using FluentAssertions;
using Xunit;

namespace FinanceTracker.IntegrationTests;

public class TransactionFlowTests : IClassFixture<TestWebApp>
{
    private readonly TestWebApp _app;

    public TransactionFlowTests(TestWebApp app) { _app = app; }

    private async Task<HttpClient> RegisterClientAsync(string? emailSeed = null)
    {
        var http = _app.CreateClient();
        var email = $"u{emailSeed ?? Guid.NewGuid().ToString("N")}@example.com";
        var resp = await http.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, "Password1!", "Test User", "INR"));
        resp.EnsureSuccessStatusCode();
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return http;
    }

    [Fact]
    public async Task SeededCategories_AreReturned()
    {
        var http = await RegisterClientAsync();
        var cats = await http.GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories");
        cats.Should().NotBeNull();
        cats!.Should().HaveCountGreaterThan(10);
        cats.Should().Contain(c => c.Type == "Income");
        cats.Should().Contain(c => c.Type == "Expense");
    }

    [Fact]
    public async Task Create_Update_Delete_Transaction_Works()
    {
        var http = await RegisterClientAsync();
        var cats = (await http.GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories"))!;
        var expense = cats.First(c => c.Type == "Expense");

        var create = await http.PostAsJsonAsync("/api/v1/transactions",
            new CreateTransactionRequest(expense.Id, null, 99.99m, "Expense", "Snack", DateTimeOffset.UtcNow));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var tx = (await create.Content.ReadFromJsonAsync<TransactionDto>())!;
        tx.Amount.Should().Be(99.99m);
        tx.Status.Should().Be("AutoApproved");

        var update = await http.PutAsJsonAsync($"/api/v1/transactions/{tx.Id}",
            new UpdateTransactionRequest(expense.Id, null, 12.50m, "Expense", "Edited", DateTimeOffset.UtcNow));
        update.IsSuccessStatusCode.Should().BeTrue();
        var updated = (await update.Content.ReadFromJsonAsync<TransactionDto>())!;
        updated.Amount.Should().Be(12.50m);
        updated.Note.Should().Be("Edited");

        var del = await http.DeleteAsync($"/api/v1/transactions/{tx.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await http.GetFromJsonAsync<TransactionListResponse>("/api/v1/transactions");
        list!.Total.Should().Be(0);
    }

    [Fact]
    public async Task TenantIsolation_OtherUserCannotSeeMyTransactions()
    {
        var alice = await RegisterClientAsync("alice");
        var bob = await RegisterClientAsync("bob");

        var cats = (await alice.GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories"))!;
        var cat = cats.First(c => c.Type == "Expense");
        var create = await alice.PostAsJsonAsync("/api/v1/transactions",
            new CreateTransactionRequest(cat.Id, null, 25m, "Expense", "Coffee", DateTimeOffset.UtcNow));
        create.IsSuccessStatusCode.Should().BeTrue();

        var bobList = await bob.GetFromJsonAsync<TransactionListResponse>("/api/v1/transactions");
        bobList!.Total.Should().Be(0);
    }

    [Fact]
    public async Task Budget_StatusReflectsSpending()
    {
        var http = await RegisterClientAsync();
        var cats = (await http.GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories"))!;
        var cat = cats.First(c => c.Type == "Expense");

        await http.PostAsJsonAsync("/api/v1/budgets",
            new CreateBudgetRequest(cat.Id, 1000m, "Monthly", null, null));
        await http.PostAsJsonAsync("/api/v1/transactions",
            new CreateTransactionRequest(cat.Id, null, 250m, "Expense", "x", DateTimeOffset.UtcNow));

        var status = (await http.GetFromJsonAsync<List<BudgetStatusDto>>("/api/v1/budgets/status"))!;
        status.Should().ContainSingle();
        status[0].SpentAmount.Should().Be(250m);
        status[0].RemainingAmount.Should().Be(750m);
        status[0].PercentUsed.Should().Be(25m);
    }

    [Fact]
    public async Task Dashboard_AggregatesCurrentMonth()
    {
        var http = await RegisterClientAsync();
        var cats = (await http.GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories"))!;
        var expCat = cats.First(c => c.Type == "Expense");
        var incCat = cats.First(c => c.Type == "Income");

        await http.PostAsJsonAsync("/api/v1/transactions",
            new CreateTransactionRequest(incCat.Id, null, 5000m, "Income", "Salary", DateTimeOffset.UtcNow));
        await http.PostAsJsonAsync("/api/v1/transactions",
            new CreateTransactionRequest(expCat.Id, null, 1200m, "Expense", "Rent", DateTimeOffset.UtcNow));

        var dash = (await http.GetFromJsonAsync<DashboardSummary>("/api/v1/reports/dashboard"))!;
        dash.IncomeMonth.Should().Be(5000m);
        dash.ExpenseMonth.Should().Be(1200m);
        dash.NetMonth.Should().Be(3800m);
        dash.TransactionCountMonth.Should().Be(2);
        dash.RecentTransactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task NegativeAmount_RejectedAtApi()
    {
        var http = await RegisterClientAsync();
        var cats = (await http.GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories"))!;
        var cat = cats.First(c => c.Type == "Expense");
        var resp = await http.PostAsJsonAsync("/api/v1/transactions",
            new CreateTransactionRequest(cat.Id, null, -10m, "Expense", null, DateTimeOffset.UtcNow));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
