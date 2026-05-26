using System.Text;
using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Domain;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Features.Transactions;

public interface ITransactionService
{
    Task<Result<TransactionListResponse>> ListAsync(TransactionQuery query, CancellationToken ct = default);
    Task<Result<TransactionListResponse>> ListPendingAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<TransactionDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<TransactionDto>> CreateAsync(CreateTransactionRequest req, CancellationToken ct = default);
    Task<Result<TransactionDto>> UpdateAsync(Guid id, UpdateTransactionRequest req, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<TransactionDto>> ApproveAsync(Guid id, CancellationToken ct = default);
    Task<Result<TransactionDto>> RejectAsync(Guid id, RejectTransactionRequest req, CancellationToken ct = default);
    Task<Result<string>> ExportCsvAsync(TransactionQuery query, CancellationToken ct = default);
    Task<Result<CsvImportResult>> ImportCsvAsync(string csvContent, bool commit, CancellationToken ct = default);
}

public class TransactionService : ITransactionService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IPermissionService _perms;
    private readonly IAuditLogService _audit;

    public TransactionService(IAppDbContext db, ICurrentUser current, IPermissionService perms, IAuditLogService audit)
    {
        _db = db;
        _current = current;
        _perms = perms;
        _audit = audit;
    }

    public async Task<Result<TransactionListResponse>> ListAsync(TransactionQuery query, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<TransactionListResponse>.Unauthorized();

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var q = _db.Transactions.AsNoTracking().AsQueryable();
        if (query.From.HasValue) q = q.Where(t => t.Date >= query.From.Value);
        if (query.To.HasValue) q = q.Where(t => t.Date <= query.To.Value);
        if (query.CategoryId.HasValue) q = q.Where(t => t.CategoryId == query.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(query.Type) && Enum.TryParse<TransactionType>(query.Type, true, out var t))
            q = q.Where(x => x.Type == t);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<TransactionStatus>(query.Status, true, out var s))
            q = q.Where(x => x.Status == s);

        var total = await q.CountAsync(ct);

        var items = await (
            from tr in q.OrderByDescending(x => x.Date).ThenByDescending(x => x.CreatedAt)
            join c in _db.Categories on tr.CategoryId equals c.Id
            join u in _db.Users.IgnoreQueryFilters() on tr.SubmittedByUserId equals u.Id
            select new TransactionListItem(
                tr.Id, c.Id, c.Name, c.Icon, c.Color,
                tr.Amount, tr.Type.ToString(), tr.Status.ToString(),
                tr.Note, tr.Date, u.Id, u.FullName)
        )
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

        return Result<TransactionListResponse>.Success(new TransactionListResponse(items, page, pageSize, total));
    }

    public Task<Result<TransactionListResponse>> ListPendingAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Task.FromResult(Result<TransactionListResponse>.Unauthorized());
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.TransactionApprove))
            return Task.FromResult(Result<TransactionListResponse>.Forbidden());

        return ListAsync(new TransactionQuery(null, null, null, null, TransactionStatus.PendingApproval.ToString(), page, pageSize), ct);
    }

    public async Task<Result<TransactionDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<TransactionDto>.Unauthorized();
        var dto = await ProjectDtoQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
        return dto is null ? Result<TransactionDto>.NotFound() : Result<TransactionDto>.Success(dto);
    }

    public async Task<Result<TransactionDto>> CreateAsync(CreateTransactionRequest req, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated || _current.UserId is null || _current.OrganisationId is null)
            return Result<TransactionDto>.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.TransactionCreate))
            return Result<TransactionDto>.Forbidden();

        if (!Enum.TryParse<TransactionType>(req.Type, true, out var type))
            return Result<TransactionDto>.Validation("Type must be Income, Expense, or Transfer");

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == req.CategoryId, ct);
        if (category is null) return Result<TransactionDto>.Validation("Category not found");

        if (req.AccountId.HasValue)
        {
            var accountExists = await _db.Accounts.AnyAsync(a => a.Id == req.AccountId.Value, ct);
            if (!accountExists) return Result<TransactionDto>.Validation("Account not found");
        }

        var org = await _db.Organisations.IgnoreQueryFilters()
            .FirstAsync(o => o.Id == _current.OrganisationId.Value, ct);

        // Resolve department for the submitting member (Enterprise mode only)
        Guid? departmentId = null;
        if (org.Mode == OrganisationMode.Enterprise)
        {
            var member = await _db.OrganisationMembers
                .FirstOrDefaultAsync(m => m.OrganisationId == org.Id && m.UserId == _current.UserId.Value, ct);
            departmentId = member?.DepartmentId;
        }

        try
        {
            var tx = Transaction.Create(
                org.Id, _current.UserId.Value, req.CategoryId, req.AccountId,
                req.Amount, type, req.Note, req.Date, org.Mode, departmentId);
            _db.Transactions.Add(tx);
            await _db.SaveChangesAsync(ct);

            if (org.Mode == OrganisationMode.Enterprise)
                await _audit.LogAsync("transaction.created", "Transaction", tx.Id, null, new { tx.Amount, tx.Type, tx.Status, tx.CategoryId }, ct);

            var dto = await ProjectDtoQuery().FirstAsync(x => x.Id == tx.Id, ct);
            return Result<TransactionDto>.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result<TransactionDto>.Validation(ex.Message);
        }
    }

    public async Task<Result<TransactionDto>> UpdateAsync(Guid id, UpdateTransactionRequest req, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<TransactionDto>.Unauthorized();
        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tx is null) return Result<TransactionDto>.NotFound();

        // Only submitter or admin/owner can edit (in Enterprise mode); always allowed in Individual
        if (_current.OrganisationMode == OrganisationMode.Enterprise
            && tx.SubmittedByUserId != _current.UserId
            && _current.Role is not (MemberRole.Owner or MemberRole.Admin))
        {
            return Result<TransactionDto>.Forbidden();
        }

        if (!Enum.TryParse<TransactionType>(req.Type, true, out var type))
            return Result<TransactionDto>.Validation("Type must be Income, Expense, or Transfer");

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == req.CategoryId, ct);
        if (!categoryExists) return Result<TransactionDto>.Validation("Category not found");

        if (req.AccountId.HasValue)
        {
            var accountExists = await _db.Accounts.AnyAsync(a => a.Id == req.AccountId.Value, ct);
            if (!accountExists) return Result<TransactionDto>.Validation("Account not found");
        }

        try
        {
            tx.Update(req.CategoryId, req.AccountId, req.Amount, type, req.Note, req.Date);
            await _db.SaveChangesAsync(ct);
            var dto = await ProjectDtoQuery().FirstAsync(x => x.Id == tx.Id, ct);
            return Result<TransactionDto>.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result<TransactionDto>.Validation(ex.Message);
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result.Unauthorized();
        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tx is null) return Result.NotFound();

        // Enterprise: submitter can delete own; admin/owner can delete any.
        if (_current.OrganisationMode == OrganisationMode.Enterprise
            && tx.SubmittedByUserId != _current.UserId
            && _current.Role is not (MemberRole.Owner or MemberRole.Admin))
        {
            return Result.Forbidden();
        }

        _db.Transactions.Remove(tx);
        await _db.SaveChangesAsync(ct);

        if (_current.OrganisationMode == OrganisationMode.Enterprise)
            await _audit.LogAsync("transaction.deleted", "Transaction", id, new { tx.Amount, tx.Type, tx.Status }, null, ct);

        return Result.Success();
    }

    public async Task<Result<TransactionDto>> ApproveAsync(Guid id, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated || _current.UserId is null) return Result<TransactionDto>.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.TransactionApprove))
            return Result<TransactionDto>.Forbidden();

        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tx is null) return Result<TransactionDto>.NotFound();

        try
        {
            var oldStatus = tx.Status.ToString();
            tx.Approve(_current.UserId.Value);
            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync("transaction.approved", "Transaction", tx.Id, new { Status = oldStatus }, new { Status = tx.Status.ToString() }, ct);
            var dto = await ProjectDtoQuery().FirstAsync(x => x.Id == tx.Id, ct);
            return Result<TransactionDto>.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result<TransactionDto>.Validation(ex.Message);
        }
    }

    public async Task<Result<TransactionDto>> RejectAsync(Guid id, RejectTransactionRequest req, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated || _current.UserId is null) return Result<TransactionDto>.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.TransactionApprove))
            return Result<TransactionDto>.Forbidden();

        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tx is null) return Result<TransactionDto>.NotFound();

        try
        {
            var oldStatus = tx.Status.ToString();
            tx.Reject(_current.UserId.Value, req.Reason);
            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync("transaction.rejected", "Transaction", tx.Id, new { Status = oldStatus }, new { Status = tx.Status.ToString(), tx.RejectionReason }, ct);
            var dto = await ProjectDtoQuery().FirstAsync(x => x.Id == tx.Id, ct);
            return Result<TransactionDto>.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result<TransactionDto>.Validation(ex.Message);
        }
    }

    public async Task<Result<string>> ExportCsvAsync(TransactionQuery query, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<string>.Unauthorized();

        var q = _db.Transactions.AsNoTracking().AsQueryable();
        if (query.From.HasValue) q = q.Where(t => t.Date >= query.From.Value);
        if (query.To.HasValue) q = q.Where(t => t.Date <= query.To.Value);
        if (query.CategoryId.HasValue) q = q.Where(t => t.CategoryId == query.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(query.Type) && Enum.TryParse<TransactionType>(query.Type, true, out var ty))
            q = q.Where(x => x.Type == ty);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<TransactionStatus>(query.Status, true, out var st))
            q = q.Where(x => x.Status == st);

        var rows = await (
            from tr in q.OrderByDescending(x => x.Date).ThenByDescending(x => x.CreatedAt)
            join c in _db.Categories on tr.CategoryId equals c.Id
            select new { tr.Date, Type = tr.Type.ToString(), Category = c.Name, tr.Amount, Status = tr.Status.ToString(), tr.Note }
        ).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.Append(Csv.Row("Date", "Type", "Category", "Amount", "Status", "Note")).Append('\n');
        foreach (var r in rows)
            sb.Append(Csv.Row(
                r.Date.ToString("yyyy-MM-dd"),
                r.Type, r.Category,
                r.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                r.Status, r.Note)).Append('\n');

        return Result<string>.Success(sb.ToString());
    }

    public async Task<Result<CsvImportResult>> ImportCsvAsync(string csvContent, bool commit, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated || _current.UserId is null || _current.OrganisationId is null)
            return Result<CsvImportResult>.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.TransactionCreate))
            return Result<CsvImportResult>.Forbidden();

        var parsed = Csv.Parse(csvContent ?? string.Empty);
        if (parsed.Count == 0)
            return Result<CsvImportResult>.Validation("CSV is empty");

        var header = parsed[0].Select(h => h.Trim().ToLowerInvariant()).ToList();
        int Col(string name) => header.IndexOf(name);
        var iDate = Col("date");
        var iType = Col("type");
        var iCategory = Col("category");
        var iAmount = Col("amount");
        var iNote = Col("note");
        if (iDate < 0 || iType < 0 || iCategory < 0 || iAmount < 0)
            return Result<CsvImportResult>.Validation("CSV must have Date, Type, Category and Amount columns");

        var org = await _db.Organisations.IgnoreQueryFilters()
            .FirstAsync(o => o.Id == _current.OrganisationId.Value, ct);

        Guid? departmentId = null;
        if (org.Mode == OrganisationMode.Enterprise)
        {
            var member = await _db.OrganisationMembers
                .FirstOrDefaultAsync(m => m.OrganisationId == org.Id && m.UserId == _current.UserId.Value, ct);
            departmentId = member?.DepartmentId;
        }

        var categories = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(c => c.Name.ToLowerInvariant(), c => c.Id, ct);

        var errors = new List<CsvRowError>();
        var toAdd = new List<Transaction>();

        for (var r = 1; r < parsed.Count; r++)
        {
            var line = r + 1; // 1-based, header is line 1
            var cells = parsed[r];

            string Get(int idx) => idx >= 0 && idx < cells.Count ? cells[idx].Trim() : string.Empty;

            var typeRaw = Get(iType);
            if (!Enum.TryParse<TransactionType>(typeRaw, true, out var type))
            { errors.Add(new CsvRowError(line, $"Invalid type '{typeRaw}'")); continue; }

            var catRaw = Get(iCategory);
            if (!categories.TryGetValue(catRaw.ToLowerInvariant(), out var categoryId))
            { errors.Add(new CsvRowError(line, $"Unknown category '{catRaw}'")); continue; }

            var amountRaw = Get(iAmount);
            if (!decimal.TryParse(amountRaw, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount) || amount <= 0m)
            { errors.Add(new CsvRowError(line, $"Invalid amount '{amountRaw}'")); continue; }

            var dateRaw = Get(iDate);
            if (!DateTimeOffset.TryParse(dateRaw, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal, out var date))
            { errors.Add(new CsvRowError(line, $"Invalid date '{dateRaw}'")); continue; }

            try
            {
                toAdd.Add(Transaction.Create(
                    org.Id, _current.UserId.Value, categoryId, null,
                    amount, type, Get(iNote), date, org.Mode, departmentId, TransactionSource.Csv));
            }
            catch (DomainException ex)
            {
                errors.Add(new CsvRowError(line, ex.Message));
            }
        }

        var dataRows = parsed.Count - 1;
        if (commit && toAdd.Count > 0)
        {
            _db.Transactions.AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
            if (org.Mode == OrganisationMode.Enterprise)
                await _audit.LogAsync("transaction.imported", "Transaction", org.Id, null, new { Count = toAdd.Count, Source = "Csv" }, ct);
        }

        return Result<CsvImportResult>.Success(new CsvImportResult(
            dataRows, commit ? toAdd.Count : 0, errors.Count, commit, errors));
    }

    private IQueryable<TransactionDto> ProjectDtoQuery()
        => from tr in _db.Transactions.AsNoTracking()
           join c in _db.Categories on tr.CategoryId equals c.Id
           join a in _db.Accounts on tr.AccountId equals a.Id into accGroup
           from a in accGroup.DefaultIfEmpty()
           select new TransactionDto(
               tr.Id, c.Id, c.Name, c.Icon, c.Color,
               tr.AccountId, a != null ? a.Name : null,
               tr.Amount, tr.Type.ToString(), tr.Status.ToString(),
               tr.Note, tr.Date, tr.Source.ToString());
}
