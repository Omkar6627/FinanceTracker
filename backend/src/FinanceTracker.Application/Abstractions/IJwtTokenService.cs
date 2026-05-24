using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Abstractions;

public record AuthTokens(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);

public interface IJwtTokenService
{
    AuthTokens IssueTokens(User user, Organisation organisation, OrganisationMember membership);
    Guid? ValidateRefreshToken(string refreshToken);
}
