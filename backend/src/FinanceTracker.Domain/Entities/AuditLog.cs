using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = default!;
    public string EntityType { get; private set; } = default!;
    public Guid EntityId { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        Guid organisationId,
        Guid actorUserId,
        string action,
        string entityType,
        Guid entityId,
        string? oldValue = null,
        string? newValue = null)
    {
        if (organisationId == Guid.Empty) throw new DomainException("OrganisationId required");
        if (string.IsNullOrWhiteSpace(action)) throw new DomainException("Action required");
        if (string.IsNullOrWhiteSpace(entityType)) throw new DomainException("EntityType required");
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
