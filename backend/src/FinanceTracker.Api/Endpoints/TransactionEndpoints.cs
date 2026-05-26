using System.Text;
using FinanceTracker.Application.Common;
using FinanceTracker.Application.Features.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Endpoints;

public static class TransactionEndpoints
{
    private const string Base = "/api/v1/transactions";

    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Base, async (
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] Guid? categoryId,
            [FromQuery] string? type,
            [FromQuery] string? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ITransactionService svc,
            CancellationToken ct) =>
        {
            var q = new TransactionQuery(from, to, categoryId, type, status, page ?? 1, pageSize ?? 25);
            return (await svc.ListAsync(q, ct)).ToHttpResult();
        }).WithTags("Transactions").RequireAuthorization();

        app.MapGet($"{Base}/pending", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ITransactionService svc,
            CancellationToken ct) =>
            (await svc.ListPendingAsync(page ?? 1, pageSize ?? 25, ct)).ToHttpResult())
            .WithTags("Transactions").RequireAuthorization();

        app.MapGet($"{Base}/export.csv", async (
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] Guid? categoryId,
            [FromQuery] string? type,
            [FromQuery] string? status,
            ITransactionService svc,
            CancellationToken ct) =>
        {
            var q = new TransactionQuery(from, to, categoryId, type, status, 1, int.MaxValue);
            var result = await svc.ExportCsvAsync(q, ct);
            if (!result.IsSuccess) return ((Result)result).ToHttpResult();
            var bytes = Encoding.UTF8.GetBytes(result.Value!);
            return Results.File(bytes, "text/csv", $"transactions-{DateTime.UtcNow:yyyyMMdd}.csv");
        }).WithTags("Transactions").RequireAuthorization();

        app.MapPost($"{Base}/import/csv", async (
            HttpRequest request,
            [FromQuery] bool? commit,
            ITransactionService svc,
            CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new ResultExtensions.ErrorBody("Expected multipart/form-data with a 'file' field"));
            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
                return Results.BadRequest(new ResultExtensions.ErrorBody("No file uploaded"));
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            return (await svc.ImportCsvAsync(content, commit ?? false, ct)).ToHttpResult();
        }).WithTags("Transactions").RequireAuthorization();

        app.MapGet($"{Base}/{{id:guid}}", async (Guid id, ITransactionService svc, CancellationToken ct) =>
            (await svc.GetAsync(id, ct)).ToHttpResult())
            .WithTags("Transactions").RequireAuthorization();

        app.MapPost(Base, async ([FromBody] CreateTransactionRequest req, ITransactionService svc, CancellationToken ct) =>
            (await svc.CreateAsync(req, ct)).ToHttpResult(StatusCodes.Status201Created))
            .WithTags("Transactions").RequireAuthorization();

        app.MapPut($"{Base}/{{id:guid}}", async (Guid id, [FromBody] UpdateTransactionRequest req, ITransactionService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(id, req, ct)).ToHttpResult())
            .WithTags("Transactions").RequireAuthorization();

        app.MapDelete($"{Base}/{{id:guid}}", async (Guid id, ITransactionService svc, CancellationToken ct) =>
            (await svc.DeleteAsync(id, ct)).ToHttpResult())
            .WithTags("Transactions").RequireAuthorization();

        app.MapPost($"{Base}/{{id:guid}}/approve", async (Guid id, ITransactionService svc, CancellationToken ct) =>
            (await svc.ApproveAsync(id, ct)).ToHttpResult())
            .WithTags("Transactions").RequireAuthorization();

        app.MapPost($"{Base}/{{id:guid}}/reject", async (Guid id, [FromBody] RejectTransactionRequest req, ITransactionService svc, CancellationToken ct) =>
            (await svc.RejectAsync(id, req, ct)).ToHttpResult())
            .WithTags("Transactions").RequireAuthorization();

        return app;
    }
}
