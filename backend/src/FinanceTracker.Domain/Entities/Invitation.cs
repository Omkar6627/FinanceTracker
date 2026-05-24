using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

public class Invitation
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public string Email { get; private set; } = default!;
    public MemberRole Role { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public string Token { get; private set; } = default!;
    public InvitationStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid InvitedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    private Invitation() { }

    public static Invitation Create(
        Guid organisationId,
        string email,
        MemberRole role,
        Guid invitedByUserId,
        Guid? departmentId,
        TimeSpan ttl,
        string token)
    {
        if (organisationId == Guid.Empty) throw new DomainException("OrganisationId required");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email required");
        if (invitedByUserId == Guid.Empty) throw new DomainException("Inviter required");
        if (string.IsNullOrWhiteSpace(token)) throw new DomainException("Token required");
        if (role == MemberRole.Owner) throw new DomainException("Cannot invite as Owner");

        return new Invitation
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            Email = email.Trim().ToLowerInvariant(),
            Role = role,
            DepartmentId = departmentId,
            Token = token,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl),
            InvitedByUserId = invitedByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Accept()
    {
        if (Status != InvitationStatus.Pending) throw new DomainException("Invitation is not pending");
        if (DateTimeOffset.UtcNow > ExpiresAt) throw new DomainException("Invitation has expired");
        Status = InvitationStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        if (Status == InvitationStatus.Pending) Status = InvitationStatus.Expired;
    }

    public bool IsValid() => Status == InvitationStatus.Pending && DateTimeOffset.UtcNow <= ExpiresAt;
}
