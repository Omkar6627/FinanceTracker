using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Features.Audit;

public record AuditEntryDto(
    Guid Id,
    Guid ActorUserId,
    string ActorName,
    string Action,
    string EntityType,
    Guid EntityId,
    string? OldValue,
    string? NewValue,
    DateTimeOffset OccurredAt);

public record AuditListResponse(
    IReadOnlyList<AuditEntryDto> Items,
    int Page,
    int PageSize,
    int Total);

public record AuditQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    Guid? ActorId,
    string? Action,
    int Page = 1,
    int PageSize = 50);

public interface IAuditQueryService
{
    Task<Result<AuditListResponse>> ListAsync(AuditQuery query, CancellationToken ct = default);
}

public class AuditQueryService : IAuditQueryService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IPermissionService _perms;

    public AuditQueryService(IAppDbContext db, ICurrentUser current, IPermissionService perms)
    {
        _db = db;
        _current = current;
        _perms = perms;
    }

    public async Task<Result<AuditListResponse>> ListAsync(AuditQuery query, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<AuditListResponse>.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.AuditView))
            return Result<AuditListResponse>.Forbidden();

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var q = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (query.From.HasValue) q = q.Where(a => a.OccurredAt >= query.From.Value);
        if (query.To.HasValue) q = q.Where(a => a.OccurredAt <= query.To.Value);
        if (query.ActorId.HasValue) q = q.Where(a => a.ActorUserId == query.ActorId.Value);
        if (!string.IsNullOrWhiteSpace(query.Action)) q = q.Where(a => a.Action.StartsWith(query.Action));

        var total = await q.CountAsync(ct);

        var items = await (
            from a in q.OrderByDescending(x => x.OccurredAt)
            join u in _db.Users.IgnoreQueryFilters() on a.ActorUserId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            select new AuditEntryDto(
                a.Id, a.ActorUserId, u != null ? u.FullName : "(unknown)",
                a.Action, a.EntityType, a.EntityId,
                a.OldValue, a.NewValue, a.OccurredAt)
        ).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Result<AuditListResponse>.Success(new AuditListResponse(items, page, pageSize, total));
    }
}
