using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Domain;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Features.Reports;

public interface IReportService
{
    Task<Result<DashboardSummary>> GetDashboardAsync(CancellationToken ct = default);
    Task<Result<MonthlyReport>> GetMonthlyAsync(int year, int month, CancellationToken ct = default);
    Task<Result<TrendReport>> GetTrendsAsync(int months, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DepartmentSummary>>> GetDepartmentSummaryAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);
}

public class ReportService : IReportService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;

    public ReportService(IAppDbContext db, ICurrentUser current)
    {
        _db = db;
        _current = current;
    }

    public async Task<Result<DashboardSummary>> GetDashboardAsync(CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<DashboardSummary>.Unauthorized();

        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1).AddTicks(-1);

        var monthTx = await _db.Transactions.AsNoTracking()
            .Where(t => t.Date >= monthStart && t.Date <= monthEnd
                        && (t.Status == TransactionStatus.Approved || t.Status == TransactionStatus.AutoApproved))
            .Select(t => new { t.Type, t.Amount, t.CategoryId, t.Id, t.Date, t.Note })
            .ToListAsync(ct);

        var incomeMonth = monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expenseMonth = monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var txCount = monthTx.Count;

        var todayTx = monthTx.Where(t => t.Date >= todayStart && t.Date <= todayEnd).ToList();
        var incomeToday = todayTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expenseToday = todayTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var catMap = await _db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, ct);

        var top = monthTx.Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Total = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Total)
            .Take(5)
            .Select(x => new
            {
                x.CategoryId,
                x.Total,
                Cat = catMap.GetValueOrDefault(x.CategoryId)
            })
            .Where(x => x.Cat is not null)
            .Select(x => new CategorySlice(
                x.CategoryId, x.Cat!.Name, x.Cat.Icon, x.Cat.Color,
                x.Total,
                expenseMonth == 0 ? 0 : decimal.Round(x.Total / expenseMonth * 100m, 1)))
            .ToList();

        var recent = await (
            from tr in _db.Transactions.AsNoTracking()
            join c in _db.Categories on tr.CategoryId equals c.Id
            where (tr.Status == TransactionStatus.Approved || tr.Status == TransactionStatus.AutoApproved)
            orderby tr.Date descending, tr.CreatedAt descending
            select new RecentTransaction(tr.Id, c.Name, c.Icon, c.Color, tr.Amount, tr.Type.ToString(), tr.Note, tr.Date)
        ).Take(5).ToListAsync(ct);

        return Result<DashboardSummary>.Success(new DashboardSummary(
            incomeMonth, expenseMonth, incomeMonth - expenseMonth, txCount,
            incomeToday, expenseToday, top, recent));
    }

    public async Task<Result<MonthlyReport>> GetMonthlyAsync(int year, int month, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<MonthlyReport>.Unauthorized();
        if (month < 1 || month > 12) return Result<MonthlyReport>.Validation("Month must be 1-12");

        var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1).AddTicks(-1);

        var tx = await _db.Transactions.AsNoTracking()
            .Where(t => t.Date >= start && t.Date <= end
                        && (t.Status == TransactionStatus.Approved || t.Status == TransactionStatus.AutoApproved))
            .Select(t => new { t.Date, t.Type, t.Amount, t.CategoryId })
            .ToListAsync(ct);

        var income = tx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expense = tx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var daily = Enumerable.Range(1, daysInMonth).Select(d =>
        {
            var date = new DateTime(year, month, d);
            var dayTx = tx.Where(t => t.Date.Day == d).ToList();
            var i = dayTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var e = dayTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            return new DayPoint(date, i, e);
        }).ToList();

        var cats = await _db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, ct);

        var expenseByCat = tx.Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CatId = g.Key, Total = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Total)
            .Where(x => cats.ContainsKey(x.CatId))
            .Select(x => new CategorySlice(x.CatId, cats[x.CatId].Name, cats[x.CatId].Icon, cats[x.CatId].Color,
                x.Total, expense == 0 ? 0 : decimal.Round(x.Total / expense * 100m, 1)))
            .ToList();

        var incomeByCat = tx.Where(t => t.Type == TransactionType.Income)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CatId = g.Key, Total = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Total)
            .Where(x => cats.ContainsKey(x.CatId))
            .Select(x => new CategorySlice(x.CatId, cats[x.CatId].Name, cats[x.CatId].Icon, cats[x.CatId].Color,
                x.Total, income == 0 ? 0 : decimal.Round(x.Total / income * 100m, 1)))
            .ToList();

        return Result<MonthlyReport>.Success(new MonthlyReport(
            year, month, income, expense, income - expense, daily, expenseByCat, incomeByCat));
    }

    public async Task<Result<TrendReport>> GetTrendsAsync(int months, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<TrendReport>.Unauthorized();
        months = Math.Clamp(months, 1, 24);

        var now = DateTimeOffset.UtcNow;
        var firstMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-(months - 1));
        var lastMonthEnd = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1).AddTicks(-1);

        var tx = await _db.Transactions.AsNoTracking()
            .Where(t => t.Date >= firstMonthStart && t.Date <= lastMonthEnd
                        && (t.Status == TransactionStatus.Approved || t.Status == TransactionStatus.AutoApproved))
            .Select(t => new { t.Date, t.Type, t.Amount })
            .ToListAsync(ct);

        var points = Enumerable.Range(0, months).Select(offset =>
        {
            var ms = firstMonthStart.AddMonths(offset);
            var me = ms.AddMonths(1).AddTicks(-1);
            var bucket = tx.Where(t => t.Date >= ms && t.Date <= me).ToList();
            var i = bucket.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var e = bucket.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            return new TrendPoint(ms.Year, ms.Month, i, e, i - e);
        }).ToList();

        return Result<TrendReport>.Success(new TrendReport(points));
    }

    public async Task<Result<IReadOnlyList<DepartmentSummary>>> GetDepartmentSummaryAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<IReadOnlyList<DepartmentSummary>>.Unauthorized();

        var fromDate = from ?? DateTimeOffset.UtcNow.AddDays(-30);
        var toDate = to ?? DateTimeOffset.UtcNow;

        var tx = await _db.Transactions.AsNoTracking()
            .Where(t => t.Date >= fromDate && t.Date <= toDate
                        && (t.Status == TransactionStatus.Approved || t.Status == TransactionStatus.AutoApproved))
            .Select(t => new { t.Type, t.Amount, t.DepartmentId })
            .ToListAsync(ct);

        var depts = await _db.Departments.AsNoTracking().Where(d => d.IsActive).ToListAsync(ct);

        var rows = new List<DepartmentSummary>(depts.Count + 1);
        foreach (var d in depts)
        {
            var bucket = tx.Where(t => t.DepartmentId == d.Id).ToList();
            var income = bucket.Where(b => b.Type == TransactionType.Income).Sum(b => b.Amount);
            var expense = bucket.Where(b => b.Type == TransactionType.Expense).Sum(b => b.Amount);
            rows.Add(new DepartmentSummary(d.Id, d.Name, income, expense, income - expense, bucket.Count));
        }

        var unassigned = tx.Where(t => t.DepartmentId == null).ToList();
        if (unassigned.Count > 0)
        {
            var i = unassigned.Where(b => b.Type == TransactionType.Income).Sum(b => b.Amount);
            var e = unassigned.Where(b => b.Type == TransactionType.Expense).Sum(b => b.Amount);
            rows.Add(new DepartmentSummary(null, "(Unassigned)", i, e, i - e, unassigned.Count));
        }

        return Result<IReadOnlyList<DepartmentSummary>>.Success(rows);
    }
}
