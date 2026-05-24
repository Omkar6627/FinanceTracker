using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

public class Organisation
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public OrganisationMode Mode { get; private set; }
    public string Currency { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Organisation() { }

    public static Organisation CreateIndividual(string ownerName, string currency = "INR")
    {
        if (string.IsNullOrWhiteSpace(ownerName)) throw new DomainException("Owner name is required");
        return new Organisation
        {
            Id = Guid.NewGuid(),
            Name = $"{ownerName.Trim()}'s Workspace",
            Mode = OrganisationMode.Individual,
            Currency = NormaliseCurrency(currency),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Organisation CreateEnterprise(string name, string currency)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Organisation name is required");
        return new Organisation
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Mode = OrganisationMode.Enterprise,
            Currency = NormaliseCurrency(currency),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new DomainException("Name is required");
        Name = newName.Trim();
    }

    public void ChangeCurrency(string currency) => Currency = NormaliseCurrency(currency);

    public void SwitchMode(OrganisationMode newMode)
    {
        if (Mode == newMode) return;
        Mode = newMode;
    }

    private static string NormaliseCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO code");
        return currency.Trim().ToUpperInvariant();
    }
}
