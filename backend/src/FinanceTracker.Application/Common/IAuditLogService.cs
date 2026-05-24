namespace FinanceTracker.Application.Common;

public interface IAuditLogService
{
    Task LogAsync(string action, string entityType, Guid entityId, object? oldValue = null, object? newValue = null, CancellationToken ct = default);
}
