using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

/// <summary>A rule that materializes a Transaction on a fixed cadence until its end date (if any).</summary>
public class RecurringTransaction
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid SubmittedByUserId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid? AccountId { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public string Note { get; private set; } = string.Empty;
    public RecurrenceFrequency Frequency { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset NextRunDate { get; private set; }
    public DateTimeOffset? EndDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? LastRunAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private RecurringTransaction() { }

    public static RecurringTransaction Create(
        Guid organisationId,
        Guid submittedByUserId,
        Guid categoryId,
        Guid? accountId,
        decimal amount,
        TransactionType type,
        string? note,
        RecurrenceFrequency frequency,
        DateTimeOffset startDate,
        DateTimeOffset? endDate = null,
        Guid? departmentId = null)
    {
        if (organisationId == Guid.Empty) throw new DomainException("OrganisationId required");
        if (submittedByUserId == Guid.Empty) throw new DomainException("Submitter required");
        if (categoryId == Guid.Empty) throw new DomainException("Category required");
        if (amount <= 0m) throw new DomainException("Amount must be greater than zero");
        if (startDate == default) throw new DomainException("Start date required");
        if (endDate.HasValue && endDate.Value <= startDate) throw new DomainException("End date must be after start date");

        return new RecurringTransaction
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            SubmittedByUserId = submittedByUserId,
            CategoryId = categoryId,
            AccountId = accountId,
            DepartmentId = departmentId,
            Amount = decimal.Round(amount, 2),
            Type = type,
            Note = (note ?? string.Empty).Trim(),
            Frequency = frequency,
            StartDate = startDate,
            NextRunDate = startDate,
            EndDate = endDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(Guid categoryId, Guid? accountId, decimal amount, TransactionType type,
        string? note, RecurrenceFrequency frequency, DateTimeOffset? endDate)
    {
        if (categoryId == Guid.Empty) throw new DomainException("Category required");
        if (amount <= 0m) throw new DomainException("Amount must be greater than zero");
        if (endDate.HasValue && endDate.Value <= StartDate) throw new DomainException("End date must be after start date");

        CategoryId = categoryId;
        AccountId = accountId;
        Amount = decimal.Round(amount, 2);
        Type = type;
        Note = (note ?? string.Empty).Trim();
        Frequency = frequency;
        EndDate = endDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>True when this rule has a due occurrence at or before <paramref name="asOf"/>.</summary>
    public bool IsDue(DateTimeOffset asOf) => IsActive && NextRunDate <= asOf;

    /// <summary>Records that the occurrence at NextRunDate was materialized and advances to the next one.</summary>
    public void MarkRun()
    {
        LastRunAt = NextRunDate;
        NextRunDate = ComputeNext(NextRunDate);
        if (EndDate.HasValue && NextRunDate > EndDate.Value)
            IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public DateTimeOffset ComputeNext(DateTimeOffset from) => Frequency switch
    {
        RecurrenceFrequency.Daily => from.AddDays(1),
        RecurrenceFrequency.Weekly => from.AddDays(7),
        RecurrenceFrequency.Monthly => from.AddMonths(1),
        _ => from.AddMonths(1)
    };
}
