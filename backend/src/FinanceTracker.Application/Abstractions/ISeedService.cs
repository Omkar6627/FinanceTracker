namespace FinanceTracker.Application.Abstractions;

public interface ISeedService
{
    Task SeedDefaultCategoriesAsync(Guid organisationId, CancellationToken ct = default);
}
