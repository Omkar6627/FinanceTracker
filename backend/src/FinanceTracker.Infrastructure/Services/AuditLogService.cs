using System.Text.Json;
using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = false };

    public AuditLogService(IAppDbContext db, ICurrentUser current)
    {
        _db = db;
        _current = current;
    }

    public async Task LogAsync(string action, string entityType, Guid entityId, object? oldValue = null, object? newValue = null, CancellationToken ct = default)
    {
        if (_current.OrganisationId is null || _current.UserId is null) return;
        var entry = AuditLog.Create(
            _current.OrganisationId.Value,
            _current.UserId.Value,
            action,
            entityType,
            entityId,
            oldValue is null ? null : JsonSerializer.Serialize(oldValue, _json),
            newValue is null ? null : JsonSerializer.Serialize(newValue, _json));
        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
