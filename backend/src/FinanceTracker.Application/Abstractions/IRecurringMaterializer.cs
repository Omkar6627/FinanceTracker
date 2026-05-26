namespace FinanceTracker.Application.Abstractions;

/// <summary>
/// Scans recurring-transaction rules across all tenants and materializes any occurrences
/// that are due at or before the given moment. Runs outside an HTTP request, so tenant
/// query filters are bypassed and each rule's own OrganisationId is used.
/// </summary>
public interface IRecurringMaterializer
{
    Task<int> RunAsync(DateTimeOffset asOf, CancellationToken ct = default);
}
