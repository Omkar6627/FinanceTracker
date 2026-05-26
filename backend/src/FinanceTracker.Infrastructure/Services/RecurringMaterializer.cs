using FinanceTracker.Application.Abstractions;
using FinanceTracker.Domain;
using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Infrastructure.Services;

public class RecurringMaterializer : IRecurringMaterializer
{
    // Guards against runaway catch-up if a rule's NextRunDate is far in the past.
    private const int MaxOccurrencesPerRule = 400;

    private readonly IAppDbContext _db;
    private readonly ILogger<RecurringMaterializer> _logger;

    public RecurringMaterializer(IAppDbContext db, ILogger<RecurringMaterializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> RunAsync(DateTimeOffset asOf, CancellationToken ct = default)
    {
        var dueRules = await _db.RecurringTransactions
            .Where(r => r.IsActive && r.NextRunDate <= asOf)
            .ToListAsync(ct);

        if (dueRules.Count == 0) return 0;

        var orgModes = await _db.Organisations
            .Where(o => dueRules.Select(r => r.OrganisationId).Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Mode, ct);

        var created = 0;
        foreach (var rule in dueRules)
        {
            if (!orgModes.TryGetValue(rule.OrganisationId, out var mode)) continue;

            var guard = 0;
            while (rule.IsDue(asOf) && guard++ < MaxOccurrencesPerRule)
            {
                var tx = Transaction.Create(
                    rule.OrganisationId, rule.SubmittedByUserId, rule.CategoryId, rule.AccountId,
                    rule.Amount, rule.Type, rule.Note, rule.NextRunDate, mode, rule.DepartmentId);
                _db.Transactions.Add(tx);
                rule.MarkRun();
                created++;
            }
        }

        if (created > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("RecurringMaterializer created {Count} transaction(s) from {Rules} rule(s)", created, dueRules.Count);
        }

        return created;
    }
}
