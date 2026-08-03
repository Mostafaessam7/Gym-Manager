using GymManager.Domain.Identity;

namespace GymManager.Application.Abstractions;

/// <summary>Issues signed JWT access tokens and opaque refresh tokens for an authenticated <see cref="User"/>.</summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IReadOnlyCollection<string> permissions);

    string GenerateRefreshToken();

    DateTimeOffset GetAccessTokenExpiration();

    DateTimeOffset GetRefreshTokenExpiration();
}
