using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Domain;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Features.Budgets;

public interface IBudgetService
{
    Task<Result<IReadOnlyList<BudgetDto>>> ListAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<BudgetStatusDto>>> GetStatusAsync(CancellationToken ct = default);
    Task<Result<BudgetDto>> CreateAsync(CreateBudgetRequest req, CancellationToken ct = default);
    Task<Result<BudgetDto>> UpdateAsync(Guid id, UpdateBudgetRequest req, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}

public class BudgetService : IBudgetService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;

    public BudgetService(IAppDbContext db, ICurrentUser current)
    {
        _db = db;
        _current = current;
    }

    public async Task<Result<IReadOnlyList<BudgetDto>>> ListAsync(CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<IReadOnlyList<BudgetDto>>.Unauthorized();
        var list = await (
            from b in _db.Budgets.AsNoTracking()
            join c in _db.Categories on b.CategoryId equals c.Id
            orderby c.Name
            select new BudgetDto(b.Id, c.Id, c.Name, c.Icon, c.Color, b.LimitAmount, b.Period.ToString(), b.StartDate, b.EndDate)
        ).ToListAsync(ct);
        return Result<IReadOnlyList<BudgetDto>>.Success(list);
    }

    public async Task<Result<IReadOnlyList<BudgetStatusDto>>> GetStatusAsync(CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<IReadOnlyList<BudgetStatusDto>>.Unauthorized();

        var now = DateTimeOffset.UtcNow;
        var budgets = await (
            from b in _db.Budgets.AsNoTracking()
            join c in _db.Categories on b.CategoryId equals c.Id
            select new { Budget = b, Category = c }
        ).ToListAsync(ct);

        var result = new List<BudgetStatusDto>();
        foreach (var x in budgets)
        {
            var (periodStart, periodEnd) = ComputePeriodWindow(x.Budget, now);
            var spent = await _db.Transactions.AsNoTracking()
                .Where(t => t.CategoryId == x.Budget.CategoryId
                            && t.Type == TransactionType.Expense
                            && (t.Status == TransactionStatus.Approved || t.Status == TransactionStatus.AutoApproved)
                            && t.Date >= periodStart && t.Date <= periodEnd)
                .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

            var limit = x.Budget.LimitAmount;
            var remaining = limit - spent;
            var pct = limit == 0 ? 0 : decimal.Round(spent / limit * 100m, 1);

            result.Add(new BudgetStatusDto(
                x.Budget.Id, x.Category.Id, x.Category.Name, x.Category.Icon, x.Category.Color,
                limit, spent, remaining, pct, x.Budget.Period.ToString(), periodStart, periodEnd));
        }

        return Result<IReadOnlyList<BudgetStatusDto>>.Success(result);
    }

    public async Task<Result<BudgetDto>> CreateAsync(CreateBudgetRequest req, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated || _current.UserId is null || _current.OrganisationId is null)
            return Result<BudgetDto>.Unauthorized();
        if (!Enum.TryParse<BudgetPeriod>(req.Period, true, out var period))
            return Result<BudgetDto>.Validation("Period must be Weekly, Monthly, or Custom");

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == req.CategoryId, ct);
        if (!categoryExists) return Result<BudgetDto>.Validation("Category not found");

        var start = req.StartDate ?? DateTimeOffset.UtcNow;
        try
        {
            var b = Budget.Create(
                _current.OrganisationId.Value, _current.UserId.Value, req.CategoryId,
                req.LimitAmount, period, start, req.EndDate);
            _db.Budgets.Add(b);
            await _db.SaveChangesAsync(ct);

            var cat = await _db.Categories.FirstAsync(c => c.Id == b.CategoryId, ct);
            return Result<BudgetDto>.Success(new BudgetDto(
                b.Id, cat.Id, cat.Name, cat.Icon, cat.Color, b.LimitAmount, b.Period.ToString(), b.StartDate, b.EndDate));
        }
        catch (DomainException ex)
        {
            return Result<BudgetDto>.Validation(ex.Message);
        }
    }

    public async Task<Result<BudgetDto>> UpdateAsync(Guid id, UpdateBudgetRequest req, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<BudgetDto>.Unauthorized();
        var b = await _db.Budgets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b is null) return Result<BudgetDto>.NotFound();
        if (!Enum.TryParse<BudgetPeriod>(req.Period, true, out var period))
            return Result<BudgetDto>.Validation("Period must be Weekly, Monthly, or Custom");
        try
        {
            b.Update(req.LimitAmount, period, req.StartDate, req.EndDate);
            await _db.SaveChangesAsync(ct);
            var cat = await _db.Categories.FirstAsync(c => c.Id == b.CategoryId, ct);
            return Result<BudgetDto>.Success(new BudgetDto(
                b.Id, cat.Id, cat.Name, cat.Icon, cat.Color, b.LimitAmount, b.Period.ToString(), b.StartDate, b.EndDate));
        }
        catch (DomainException ex)
        {
            return Result<BudgetDto>.Validation(ex.Message);
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result.Unauthorized();
        var b = await _db.Budgets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b is null) return Result.NotFound();
        _db.Budgets.Remove(b);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static (DateTimeOffset start, DateTimeOffset end) ComputePeriodWindow(Budget b, DateTimeOffset now)
    {
        switch (b.Period)
        {
            case BudgetPeriod.Weekly:
            {
                var dayOfWeek = (int)now.DayOfWeek;
                var start = new DateTimeOffset(now.Date, now.Offset).AddDays(-dayOfWeek);
                var end = start.AddDays(7).AddTicks(-1);
                return (start, end);
            }
            case BudgetPeriod.Monthly:
            {
                var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
                var end = start.AddMonths(1).AddTicks(-1);
                return (start, end);
            }
            default:
                return (b.StartDate, b.EndDate ?? now);
        }
    }
}
