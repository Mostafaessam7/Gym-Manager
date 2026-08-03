using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Identity;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary><paramref name="refreshTokenHash"/> must already be hashed by the caller (Application layer,
    /// via <c>SecureTokenHasher</c>) — see <see cref="User.IssueRefreshToken"/>.</summary>
    Task<User?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default);

    Task<User?> GetByPasswordResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<User?> GetByTwoFactorChallengeTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
