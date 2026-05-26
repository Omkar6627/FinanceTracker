using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Organisation> Organisations { get; }
    DbSet<OrganisationMember> OrganisationMembers { get; }
    DbSet<Category> Categories { get; }
    DbSet<Account> Accounts { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<Budget> Budgets { get; }
    DbSet<Department> Departments { get; }
    DbSet<Invitation> Invitations { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RecurringTransaction> RecurringTransactions { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
