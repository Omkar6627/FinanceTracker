using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid SubmittedByUserId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid? AccountId { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public string Note { get; private set; } = string.Empty;
    public DateTimeOffset Date { get; private set; }
    public string? ExternalRef { get; private set; }
    public TransactionSource Source { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Transaction() { }

    public static Transaction Create(
        Guid organisationId,
        Guid submittedByUserId,
        Guid categoryId,
        Guid? accountId,
        decimal amount,
        TransactionType type,
        string? note,
        DateTimeOffset date,
        OrganisationMode mode,
        Guid? departmentId = null,
        TransactionSource source = TransactionSource.Manual,
        string? externalRef = null)
    {
        if (organisationId == Guid.Empty) throw new DomainException("OrganisationId required");
        if (submittedByUserId == Guid.Empty) throw new DomainException("Submitter required");
        if (categoryId == Guid.Empty) throw new DomainException("Category required");
        if (amount <= 0m) throw new DomainException("Amount must be greater than zero");
        if (date == default) throw new DomainException("Date required");

        var status = mode == OrganisationMode.Individual
            ? TransactionStatus.AutoApproved
            : TransactionStatus.PendingApproval;

        return new Transaction
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            SubmittedByUserId = submittedByUserId,
            CategoryId = categoryId,
            AccountId = accountId,
            DepartmentId = departmentId,
            Amount = decimal.Round(amount, 2),
            Type = type,
            Status = status,
            Note = (note ?? string.Empty).Trim(),
            Date = date,
            Source = source,
            ExternalRef = externalRef,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(Guid categoryId, Guid? accountId, decimal amount, TransactionType type, string? note, DateTimeOffset date)
    {
        if (categoryId == Guid.Empty) throw new DomainException("Category required");
        if (amount <= 0m) throw new DomainException("Amount must be greater than zero");
        if (Status == TransactionStatus.Approved || Status == TransactionStatus.Rejected)
            throw new DomainException("Approved or rejected transactions cannot be edited");

        CategoryId = categoryId;
        AccountId = accountId;
        Amount = decimal.Round(amount, 2);
        Type = type;
        Note = (note ?? string.Empty).Trim();
        Date = date;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve(Guid approverId)
    {
        if (Status != TransactionStatus.PendingApproval)
            throw new DomainException("Only pending transactions can be approved");
        Status = TransactionStatus.Approved;
        ApprovedByUserId = approverId;
        ApprovedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(Guid approverId, string reason)
    {
        if (Status != TransactionStatus.PendingApproval)
            throw new DomainException("Only pending transactions can be rejected");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason required");
        Status = TransactionStatus.Rejected;
        ApprovedByUserId = approverId;
        ApprovedAt = DateTimeOffset.UtcNow;
        RejectionReason = reason.Trim();
    }

    public decimal SignedAmount() => Type == TransactionType.Income ? Amount : -Amount;
}
