using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FinanceTracker.Application.Common;
using FinanceTracker.Domain;
using Microsoft.AspNetCore.Http;

namespace FinanceTracker.Infrastructure.Security;

public class CurrentUser : ICurrentUser
{
    public CurrentUser(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            IsAuthenticated = true;
            var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(sub, out var uid)) UserId = uid;

            var org = user.FindFirstValue(JwtTokenService.ClaimOrgId);
            if (Guid.TryParse(org, out var oid)) OrganisationId = oid;

            Email = user.FindFirstValue(JwtRegisteredClaimNames.Email) ?? user.FindFirstValue(ClaimTypes.Email);

            var roleStr = user.FindFirstValue(JwtTokenService.ClaimRole);
            if (Enum.TryParse<MemberRole>(roleStr, out var role)) Role = role;

            var modeStr = user.FindFirstValue(JwtTokenService.ClaimOrgMode);
            if (Enum.TryParse<OrganisationMode>(modeStr, out var mode)) OrganisationMode = mode;
        }
    }

    public Guid? UserId { get; }
    public Guid? OrganisationId { get; }
    public string? Email { get; }
    public MemberRole? Role { get; }
    public OrganisationMode? OrganisationMode { get; }
    public bool IsAuthenticated { get; }
}
