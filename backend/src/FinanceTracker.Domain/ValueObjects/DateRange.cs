using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.ValueObjects;

public readonly record struct DateRange(DateTimeOffset From, DateTimeOffset To)
{
    public bool Contains(DateTimeOffset date) => date >= From && date <= To;

    public static DateRange Month(int year, int month)
    {
        if (month < 1 || month > 12)
            throw new DomainException("Month must be between 1 and 12");
        var from = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddMonths(1).AddTicks(-1);
        return new DateRange(from, to);
    }
}
