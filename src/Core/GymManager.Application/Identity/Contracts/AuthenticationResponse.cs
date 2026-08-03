namespace GymManager.Application.Identity.Contracts;

public sealed record AuthenticationResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresOnUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresOnUtc,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

/// <summary>What <c>POST /auth/login</c> returns. Exactly one of the two payloads is populated: a normal
/// sign-in yields <see cref="Authentication"/>, while an account with 2FA enabled instead yields a
/// short-lived <see cref="TwoFactorChallengeToken"/> that must be presented to the 2FA-completion endpoint
/// alongside a TOTP or recovery code before any access/refresh token is issued.</summary>
public sealed record LoginResponse(
    bool RequiresTwoFactor,
    string? TwoFactorChallengeToken,
    AuthenticationResponse? Authentication);

public sealed record TwoFactorSetupResponse(string SecretKey, string ProvisioningUri);

public sealed record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    bool IsActive,
    Guid? BranchId,
    IReadOnlyCollection<string> Roles,
    DateTimeOffset CreatedOnUtc);

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole,
    IReadOnlyCollection<string> Permissions);
