using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Application.Features.Reports;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Endpoints;

public static class ReportEndpoints
{
    private const string Base = "/api/v1/reports";

    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet($"{Base}/dashboard", async (IReportService svc, CancellationToken ct) =>
            (await svc.GetDashboardAsync(ct)).ToHttpResult())
            .WithTags("Reports").RequireAuthorization();

        app.MapGet($"{Base}/monthly", async ([FromQuery] int year, [FromQuery] int month, IReportService svc, CancellationToken ct) =>
            (await svc.GetMonthlyAsync(year, month, ct)).ToHttpResult())
            .WithTags("Reports").RequireAuthorization();

        app.MapGet($"{Base}/monthly.pdf", async ([FromQuery] int year, [FromQuery] int month, IReportPdfService pdf, CancellationToken ct) =>
        {
            var result = await pdf.RenderMonthlyAsync(year, month, ct);
            if (!result.IsSuccess) return ((Result)result).ToHttpResult();
            return Results.File(result.Value!, "application/pdf", $"finance-report-{year}-{month:D2}.pdf");
        }).WithTags("Reports").RequireAuthorization();

        app.MapGet($"{Base}/trends", async ([FromQuery] int? months, IReportService svc, CancellationToken ct) =>
            (await svc.GetTrendsAsync(months ?? 6, ct)).ToHttpResult())
            .WithTags("Reports").RequireAuthorization();

        app.MapGet($"{Base}/departments", async (
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            IReportService svc,
            CancellationToken ct) =>
            (await svc.GetDepartmentSummaryAsync(from, to, ct)).ToHttpResult())
            .WithTags("Reports").RequireAuthorization();

        return app;
    }
}
