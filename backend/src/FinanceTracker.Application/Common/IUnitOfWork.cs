namespace FinanceTracker.Application.Common;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
