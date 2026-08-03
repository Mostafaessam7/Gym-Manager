namespace GymManager.Infrastructure.Authentication;

/// <summary>Binds the <c>Jwt</c> configuration section used to issue and validate access/refresh tokens.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string SecretKey { get; init; }

    public int AccessTokenExpirationMinutes { get; init; } = 30;

    public int RefreshTokenExpirationDays { get; init; } = 7;
}
