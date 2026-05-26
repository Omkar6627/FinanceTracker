using System.Globalization;
using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Application.Features.Reports;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FinanceTracker.Infrastructure.Services;

public class ReportPdfService : IReportPdfService
{
    private const string Indigo = "#6366f1";
    private const string Green = "#10b981";
    private const string Red = "#ef4444";

    private readonly IReportService _reports;
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;

    public ReportPdfService(IReportService reports, IAppDbContext db, ICurrentUser current)
    {
        _reports = reports;
        _db = db;
        _current = current;
    }

    public async Task<Result<byte[]>> RenderMonthlyAsync(int year, int month, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated || _current.OrganisationId is null)
            return Result<byte[]>.Unauthorized();

        var reportResult = await _reports.GetMonthlyAsync(year, month, ct);
        if (!reportResult.IsSuccess)
            return reportResult.Status == ResultStatus.Validation
                ? Result<byte[]>.Validation(reportResult.Error ?? "Invalid report request")
                : Result<byte[]>.Unauthorized();

        var report = reportResult.Value!;
        var org = await _db.Organisations.IgnoreQueryFilters()
            .FirstAsync(o => o.Id == _current.OrganisationId.Value, ct);

        var bytes = Build(report, org.Name, org.Currency);
        return Result<byte[]>.Success(bytes);
    }

    private static byte[] Build(MonthlyReport r, string orgName, string currency)
    {
        string Money(decimal d) => $"{currency} {d.ToString("N2", CultureInfo.InvariantCulture)}";
        var monthLabel = new DateTime(r.Year, r.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor("#1e293b"));

                page.Header().Column(col =>
                {
                    col.Item().Text("FinanceTracker").FontSize(20).Bold().FontColor(Indigo);
                    col.Item().Text("Monthly Report").FontSize(13).SemiBold();
                    col.Item().PaddingTop(2).Text($"{orgName}  ·  {monthLabel}").FontSize(10).FontColor("#64748b");
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#e2e8f0");
                });

                page.Content().PaddingVertical(14).Column(col =>
                {
                    col.Spacing(18);

                    // Summary cards
                    col.Item().Row(row =>
                    {
                        row.Spacing(10);
                        SummaryCard(row, "Income", Money(r.TotalIncome), Green);
                        SummaryCard(row, "Expense", Money(r.TotalExpense), Red);
                        SummaryCard(row, "Net", Money(r.Net), r.Net >= 0 ? Green : Red);
                    });

                    CategorySection(col, "Expense by category", r.ExpenseByCategory, Money);
                    CategorySection(col, "Income by category", r.IncomeByCategory, Money);
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text($"Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(8).FontColor("#94a3b8");
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Page ").FontSize(8).FontColor("#94a3b8");
                        t.CurrentPageNumber().FontSize(8).FontColor("#94a3b8");
                        t.Span(" / ").FontSize(8).FontColor("#94a3b8");
                        t.TotalPages().FontSize(8).FontColor("#94a3b8");
                    });
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static void SummaryCard(RowDescriptor row, string label, string value, string color)
    {
        row.RelativeItem().Background("#f8fafc").Border(1).BorderColor("#e2e8f0").Padding(12).Column(c =>
        {
            c.Item().Text(label.ToUpperInvariant()).FontSize(8).FontColor("#64748b").LetterSpacing(0.05f);
            c.Item().PaddingTop(4).Text(value).FontSize(14).Bold().FontColor(color);
        });
    }

    private static void CategorySection(ColumnDescriptor col, string title,
        IReadOnlyList<CategorySlice> slices, Func<decimal, string> money)
    {
        col.Item().Column(section =>
        {
            section.Item().Text(title).FontSize(12).Bold();
            section.Item().PaddingTop(6).Element(e =>
            {
                if (slices.Count == 0)
                {
                    e.Text("No transactions this month.").FontSize(9).FontColor("#94a3b8").Italic();
                    return;
                }

                e.Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.ConstantColumn(60);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(HeaderCell).Text("Category");
                        h.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                        h.Cell().Element(HeaderCell).AlignRight().Text("%");
                    });

                    foreach (var s in slices)
                    {
                        table.Cell().Element(BodyCell).Text(s.CategoryName);
                        table.Cell().Element(BodyCell).AlignRight().Text(money(s.Amount));
                        table.Cell().Element(BodyCell).AlignRight().Text($"{s.Percent.ToString("0.#", CultureInfo.InvariantCulture)}%");
                    }
                });
            });
        });

        static IContainer HeaderCell(IContainer c) =>
            c.BorderBottom(1).BorderColor("#cbd5e1").PaddingVertical(4).DefaultTextStyle(t => t.SemiBold().FontSize(9).FontColor("#475569"));

        static IContainer BodyCell(IContainer c) =>
            c.BorderBottom(1).BorderColor("#f1f5f9").PaddingVertical(4).DefaultTextStyle(t => t.FontSize(10));
    }
}
