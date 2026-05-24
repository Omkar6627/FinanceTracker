using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

public class Department
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public string Name { get; private set; } = default!;
    public Guid? ManagerMemberId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Department() { }

    public static Department Create(Guid organisationId, string name, Guid? managerMemberId = null)
    {
        if (organisationId == Guid.Empty) throw new DomainException("OrganisationId required");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Department name required");
        return new Department
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            Name = name.Trim(),
            ManagerMemberId = managerMemberId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Department name required");
        Name = name.Trim();
    }

    public void AssignManager(Guid? memberId) => ManagerMemberId = memberId;
    public void Deactivate() => IsActive = false;
}
