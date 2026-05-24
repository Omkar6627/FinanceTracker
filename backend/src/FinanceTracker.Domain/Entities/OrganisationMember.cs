using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

public class OrganisationMember
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid UserId { get; private set; }
    public MemberRole Role { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private OrganisationMember() { }

    public static OrganisationMember Create(Guid organisationId, Guid userId, MemberRole role, Guid? departmentId = null)
    {
        if (organisationId == Guid.Empty) throw new DomainException("OrganisationId required");
        if (userId == Guid.Empty) throw new DomainException("UserId required");
        return new OrganisationMember
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            UserId = userId,
            Role = role,
            DepartmentId = departmentId,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void ChangeRole(MemberRole newRole) => Role = newRole;
    public void AssignDepartment(Guid? departmentId) => DepartmentId = departmentId;
    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
