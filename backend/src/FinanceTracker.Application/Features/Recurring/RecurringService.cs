using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Domain;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Features.Recurring;

public interface IRecurringService
{
    Task<Result<IReadOnlyList<RecurringDto>>> ListAsync(CancellationToken ct = default);
    Task<Result<RecurringDto>> CreateAsync(CreateRecurringRequest req, CancellationToken ct = default);
    Task<Result<RecurringDto>> UpdateAsync(Guid id, UpdateRecurringRequest req, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<RecurringDto>> RunNowAsync(Guid id, CancellationToken ct = default);
}

public class RecurringService : IRecurringService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IPermissionService _perms;

    public RecurringService(IAppDbContext db, ICurrentUser current, IPermissionService perms)
    {
        _db = db;
        _current = current;
        _perms = perms;
    }

    public async Task<Result<IReadOnlyList<RecurringDto>>> ListAsync(CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<IReadOnlyList<RecurringDto>>.Unauthorized();
        var items = await ProjectQuery()
            .OrderByDescending(x => x.IsActive).ThenBy(x => x.NextRunDate)
            .ToListAsync(ct);
        return Result<IReadOnlyList<RecurringDto>>.Success(items);
    }

    public async Task<Result<RecurringDto>> CreateAsync(CreateRecurringRequest req, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated || _current.UserId is null || _current.OrganisationId is null)
            return Result<RecurringDto>.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.TransactionCreate))
            return Result<RecurringDto>.Forbidden();

        if (!Enum.TryParse<TransactionType>(req.Type, true, out var type))
            return Result<RecurringDto>.Validation("Type must be Income, Expense, or Transfer");
        if (!Enum.TryParse<RecurrenceFrequency>(req.Frequency, true, out var freq))
            return Result<RecurringDto>.Validation("Frequency must be Daily, Weekly, or Monthly");

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == req.CategoryId, ct);
        if (!categoryExists) return Result<RecurringDto>.Validation("Category not found");
        if (req.AccountId.HasValue && !await _db.Accounts.AnyAsync(a => a.Id == req.AccountId.Value, ct))
            return Result<RecurringDto>.Validation("Account not found");

        var (org, departmentId) = await ResolveOrgAsync(ct);

        try
        {
            var rule = RecurringTransaction.Create(
                org.Id, _current.UserId.Value, req.CategoryId, req.AccountId,
                req.Amount, type, req.Note, freq, req.StartDate, req.EndDate, departmentId);
            _db.RecurringTransactions.Add(rule);
            await _db.SaveChangesAsync(ct);
            return Result<RecurringDto>.Success(await ProjectQuery().FirstAsync(x => x.Id == rule.Id, ct));
        }
        catch (DomainException ex)
        {
            return Result<RecurringDto>.Validation(ex.Message);
        }
    }

    public async Task<Result<RecurringDto>> UpdateAsync(Guid id, UpdateRecurringRequest req, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<RecurringDto>.Unauthorized();
        var rule = await _db.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (rule is null) return Result<RecurringDto>.NotFound();

        if (!Enum.TryParse<TransactionType>(req.Type, true, out var type))
            return Result<RecurringDto>.Validation("Type must be Income, Expense, or Transfer");
        if (!Enum.TryParse<RecurrenceFrequency>(req.Frequency, true, out var freq))
            return Result<RecurringDto>.Validation("Frequency must be Daily, Weekly, or Monthly");
        if (!await _db.Categories.AnyAsync(c => c.Id == req.CategoryId, ct))
            return Result<RecurringDto>.Validation("Category not found");
        if (req.AccountId.HasValue && !await _db.Accounts.AnyAsync(a => a.Id == req.AccountId.Value, ct))
            return Result<RecurringDto>.Validation("Account not found");

        try
        {
            rule.Update(req.CategoryId, req.AccountId, req.Amount, type, req.Note, freq, req.EndDate);
            rule.SetActive(req.IsActive);
            await _db.SaveChangesAsync(ct);
            return Result<RecurringDto>.Success(await ProjectQuery().FirstAsync(x => x.Id == rule.Id, ct));
        }
        catch (DomainException ex)
        {
            return Result<RecurringDto>.Validation(ex.Message);
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result.Unauthorized();
        var rule = await _db.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (rule is null) return Result.NotFound();
        _db.RecurringTransactions.Remove(rule);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<RecurringDto>> RunNowAsync(Guid id, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated || _current.UserId is null) return Result<RecurringDto>.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.TransactionCreate))
            return Result<RecurringDto>.Forbidden();

        var rule = await _db.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (rule is null) return Result<RecurringDto>.NotFound();
        if (!rule.IsActive) return Result<RecurringDto>.Validation("Rule is paused");

        var (org, _) = await ResolveOrgAsync(ct);
        try
        {
            var tx = Transaction.Create(
                rule.OrganisationId, rule.SubmittedByUserId, rule.CategoryId, rule.AccountId,
                rule.Amount, rule.Type, rule.Note, rule.NextRunDate, org.Mode, rule.DepartmentId);
            _db.Transactions.Add(tx);
            rule.MarkRun();
            await _db.SaveChangesAsync(ct);
            return Result<RecurringDto>.Success(await ProjectQuery().FirstAsync(x => x.Id == rule.Id, ct));
        }
        catch (DomainException ex)
        {
            return Result<RecurringDto>.Validation(ex.Message);
        }
    }

    private async Task<(Organisation org, Guid? departmentId)> ResolveOrgAsync(CancellationToken ct)
    {
        var org = await _db.Organisations.IgnoreQueryFilters()
            .FirstAsync(o => o.Id == _current.OrganisationId!.Value, ct);
        Guid? departmentId = null;
        if (org.Mode == OrganisationMode.Enterprise)
        {
            var member = await _db.OrganisationMembers
                .FirstOrDefaultAsync(m => m.OrganisationId == org.Id && m.UserId == _current.UserId!.Value, ct);
            departmentId = member?.DepartmentId;
        }
        return (org, departmentId);
    }

    private IQueryable<RecurringDto> ProjectQuery()
        => from r in _db.RecurringTransactions.AsNoTracking()
           join c in _db.Categories on r.CategoryId equals c.Id
           join a in _db.Accounts on r.AccountId equals a.Id into accGroup
           from a in accGroup.DefaultIfEmpty()
           select new RecurringDto(
               r.Id, c.Id, c.Name, c.Icon, c.Color,
               r.AccountId, a != null ? a.Name : null,
               r.Amount, r.Type.ToString(), r.Note, r.Frequency.ToString(),
               r.StartDate, r.NextRunDate, r.EndDate, r.IsActive, r.LastRunAt);
}
