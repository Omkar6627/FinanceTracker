using System.Text.RegularExpressions;
using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Domain;
using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Features.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest req, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshAsync(RefreshRequest req, CancellationToken ct = default);
    Task<Result<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<Result<AuthResponse>> AcceptInvitationAsync(AcceptInvitationRequest req, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly ISeedService _seed;

    public AuthService(IAppDbContext db, IPasswordHasher hasher, IJwtTokenService jwt, ISeedService seed)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _seed = seed;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(req.Email) || !EmailPattern.IsMatch(req.Email))
            errors.Add("A valid email is required");
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
            errors.Add("Password must be at least 8 characters");
        if (string.IsNullOrWhiteSpace(req.FullName) || req.FullName.Trim().Length < 2)
            errors.Add("Full name is required");
        if (errors.Count > 0) return Result<AuthResponse>.Validation(errors);

        var email = req.Email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists) return Result<AuthResponse>.Conflict("An account with this email already exists");

        var hash = _hasher.Hash(req.Password);
        var user = User.Create(email, hash, req.FullName);
        var org = Organisation.CreateIndividual(req.FullName, string.IsNullOrWhiteSpace(req.Currency) ? "INR" : req.Currency!);
        var membership = OrganisationMember.Create(org.Id, user.Id, MemberRole.Owner);

        _db.Users.Add(user);
        _db.Organisations.Add(org);
        _db.OrganisationMembers.Add(membership);
        await _db.SaveChangesAsync(ct);

        await _seed.SeedDefaultCategoriesAsync(org.Id, ct);

        var tokens = _jwt.IssueTokens(user, org, membership);
        return Result<AuthResponse>.Success(BuildResponse(tokens, user, org, membership));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Result<AuthResponse>.Validation("Email and password are required");

        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !_hasher.Verify(req.Password, user.PasswordHash))
            return Result<AuthResponse>.Unauthorized("Invalid email or password");

        var membership = await _db.OrganisationMembers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.IsActive, ct);
        if (membership is null)
            return Result<AuthResponse>.Failure("No active organisation membership found");

        var org = await _db.Organisations
            .IgnoreQueryFilters()
            .FirstAsync(o => o.Id == membership.OrganisationId, ct);

        var tokens = _jwt.IssueTokens(user, org, membership);
        return Result<AuthResponse>.Success(BuildResponse(tokens, user, org, membership));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            return Result<AuthResponse>.Validation("Refresh token is required");

        var userId = _jwt.ValidateRefreshToken(req.RefreshToken);
        if (userId is null) return Result<AuthResponse>.Unauthorized("Invalid or expired refresh token");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
        if (user is null) return Result<AuthResponse>.Unauthorized("User no longer exists");

        var membership = await _db.OrganisationMembers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.IsActive, ct);
        if (membership is null) return Result<AuthResponse>.Unauthorized("Membership missing");

        var org = await _db.Organisations
            .IgnoreQueryFilters()
            .FirstAsync(o => o.Id == membership.OrganisationId, ct);

        var tokens = _jwt.IssueTokens(user, org, membership);
        return Result<AuthResponse>.Success(BuildResponse(tokens, user, org, membership));
    }

    public async Task<Result<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Result<UserProfileDto>.NotFound("User not found");

        var membership = await _db.OrganisationMembers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.IsActive, ct);
        if (membership is null) return Result<UserProfileDto>.NotFound("Membership not found");

        var org = await _db.Organisations
            .IgnoreQueryFilters()
            .FirstAsync(o => o.Id == membership.OrganisationId, ct);

        return Result<UserProfileDto>.Success(new UserProfileDto(
            user.Id, user.Email, user.FullName,
            org.Id, org.Name, org.Mode.ToString(), org.Currency,
            membership.Role.ToString()));
    }

    public async Task<Result<AuthResponse>> AcceptInvitationAsync(AcceptInvitationRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Token)) return Result<AuthResponse>.Validation("Token is required");
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
            return Result<AuthResponse>.Validation("Password must be at least 8 characters");
        if (string.IsNullOrWhiteSpace(req.FullName) || req.FullName.Trim().Length < 2)
            return Result<AuthResponse>.Validation("Full name is required");

        var inv = await _db.Invitations.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Token == req.Token, ct);
        if (inv is null) return Result<AuthResponse>.NotFound("Invitation not found");
        if (!inv.IsValid()) return Result<AuthResponse>.Validation("Invitation is no longer valid");

        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == inv.Email, ct);
        User user;
        if (existing is null)
        {
            user = User.Create(inv.Email, _hasher.Hash(req.Password), req.FullName);
            _db.Users.Add(user);
        }
        else
        {
            user = existing;
            // Existing user accepting into a new org — keep their password as-is.
        }

        var membership = OrganisationMember.Create(inv.OrganisationId, user.Id, inv.Role, inv.DepartmentId);
        _db.OrganisationMembers.Add(membership);
        inv.Accept();
        await _db.SaveChangesAsync(ct);

        var org = await _db.Organisations.IgnoreQueryFilters().FirstAsync(o => o.Id == inv.OrganisationId, ct);
        var tokens = _jwt.IssueTokens(user, org, membership);
        return Result<AuthResponse>.Success(BuildResponse(tokens, user, org, membership));
    }

    private static AuthResponse BuildResponse(AuthTokens tokens, User user, Organisation org, OrganisationMember m)
        => new(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAt,
            new UserProfileDto(user.Id, user.Email, user.FullName,
                org.Id, org.Name, org.Mode.ToString(), org.Currency, m.Role.ToString()));
}
