namespace FinanceTracker.Application.Features.Auth;

public record RegisterRequest(string Email, string Password, string FullName, string? Currency);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record AcceptInvitationRequest(string Token, string FullName, string Password);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserProfileDto User);

public record UserProfileDto(
    Guid Id,
    string Email,
    string FullName,
    Guid OrganisationId,
    string OrganisationName,
    string OrganisationMode,
    string Currency,
    string Role);
