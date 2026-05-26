using FinanceTracker.Application.Common;

namespace FinanceTracker.Application.Abstractions;

public interface IReportPdfService
{
    Task<Result<byte[]>> RenderMonthlyAsync(int year, int month, CancellationToken ct = default);
}
