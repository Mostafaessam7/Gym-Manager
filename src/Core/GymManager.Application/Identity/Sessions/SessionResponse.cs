namespace GymManager.Application.Identity.Sessions;

public sealed record SessionResponse(
    Guid Id,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset ExpiresOnUtc,
    bool IsActive);
