using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinanceTracker.Application.Abstractions;
using FinanceTracker.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FinanceTracker.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    public const string ClaimOrgId = "org";
    public const string ClaimOrgMode = "org_mode";
    public const string ClaimRole = "role";
    public const string ClaimTokenType = "typ";

    private readonly JwtOptions _opt;

    public JwtTokenService(IOptions<JwtOptions> opt)
    {
        _opt = opt.Value;
        if (string.IsNullOrWhiteSpace(_opt.SigningKey) || _opt.SigningKey.Length < 32)
            throw new InvalidOperationException("JWT SigningKey must be at least 32 characters");
    }

    public AuthTokens IssueTokens(User user, Organisation org, OrganisationMember m)
    {
        var now = DateTime.UtcNow;
        var accessExpires = now.AddMinutes(_opt.AccessTokenMinutes);
        var access = BuildToken(user, org, m, "access", accessExpires);

        var refreshExpires = now.AddDays(_opt.RefreshTokenDays);
        var refresh = BuildToken(user, org, m, "refresh", refreshExpires);

        return new AuthTokens(access, refresh, accessExpires);
    }

    public Guid? ValidateRefreshToken(string refreshToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var parameters = BuildValidationParameters();
            var principal = handler.ValidateToken(refreshToken, parameters, out _);
            var typ = principal.FindFirstValue(ClaimTokenType);
            if (!string.Equals(typ, "refresh", StringComparison.Ordinal)) return null;
            var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
        catch
        {
            return null;
        }
    }

    public TokenValidationParameters BuildValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = _opt.Issuer,
        ValidateAudience = true,
        ValidAudience = _opt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    private string BuildToken(User user, Organisation org, OrganisationMember m, string tokenType, DateTime expires)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.FullName),
            new(ClaimOrgId, org.Id.ToString()),
            new(ClaimOrgMode, org.Mode.ToString()),
            new(ClaimRole, m.Role.ToString()),
            new(ClaimTokenType, tokenType),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
