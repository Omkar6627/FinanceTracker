using FinanceTracker.Domain;

namespace FinanceTracker.Application.Common;

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? OrganisationId { get; }
    string? Email { get; }
    MemberRole? Role { get; }
    OrganisationMode? OrganisationMode { get; }
    bool IsAuthenticated { get; }
}
