using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GymManager.Application.Abstractions;
using GymManager.Domain.Identity;
using Microsoft.IdentityModel.Tokens;

namespace GymManager.Infrastructure.Authentication;

/// <inheritdoc cref="IJwtTokenService"/>
public sealed class JwtTokenService(JwtOptions options) : IJwtTokenService
{
    public string GenerateAccessToken(User user, IReadOnlyCollection<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (user.BranchId.HasValue)
            claims.Add(new Claim("branch_id", user.BranchId.Value.ToString()));

        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: GetAccessTokenExpiration().UtcDateTime,
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public DateTimeOffset GetAccessTokenExpiration() => DateTimeOffset.UtcNow.AddMinutes(options.AccessTokenExpirationMinutes);

    public DateTimeOffset GetRefreshTokenExpiration() => DateTimeOffset.UtcNow.AddDays(options.RefreshTokenExpirationDays);
}
