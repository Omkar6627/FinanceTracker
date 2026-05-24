using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public string Name { get; private set; } = default!;
    public AccountType Type { get; private set; }
    public decimal Balance { get; private set; }
    public string? ExternalAccountId { get; private set; }
    public string? InstitutionName { get; private set; }
    public bool IsLinked { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Account() { }

    public static Account Create(Guid organisationId, string name, AccountType type, decimal openingBalance = 0m)
    {
        if (organisationId == Guid.Empty) throw new DomainException("OrganisationId required");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Account name required");
        return new Account
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            Name = name.Trim(),
            Type = type,
            Balance = openingBalance,
            IsLinked = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AdjustBalance(decimal delta) => Balance += delta;
}
