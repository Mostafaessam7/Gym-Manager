using GymManager.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(GymManagerDbContext dbContext) : IUserRepository
{
    // Roles/RefreshTokens/PasswordHistory/TwoFactorRecoveryCodes are owned collections mapped to their own
    // tables, so unlike an owned *reference* (e.g. Email) EF Core does not load them automatically — every
    // write path that mutates them (assigning a role, issuing a refresh token, recording a password change,
    // enabling 2FA) needs them explicitly included or the change tracker never sees the in-memory mutation
    // and the aggregate silently fails to persist it correctly.
    private IQueryable<User> UsersWithOwnedCollections =>
        dbContext.Users
            .Include(u => u.Roles)
            .Include(u => u.RefreshTokens)
            .Include(u => u.PasswordHistory)
            .Include(u => u.TwoFactorRecoveryCodes);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        UsersWithOwnedCollections.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        UsersWithOwnedCollections.FirstOrDefaultAsync(u => u.Email.Value == email, cancellationToken);

    public Task<User?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default) =>
        UsersWithOwnedCollections.FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == refreshTokenHash), cancellationToken);

    public Task<User?> GetByPasswordResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        UsersWithOwnedCollections.FirstOrDefaultAsync(u => u.PasswordResetTokenHash == tokenHash, cancellationToken);

    public Task<User?> GetByEmailVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        UsersWithOwnedCollections.FirstOrDefaultAsync(u => u.EmailVerificationTokenHash == tokenHash, cancellationToken);

    public Task<User?> GetByTwoFactorChallengeTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        UsersWithOwnedCollections.FirstOrDefaultAsync(u => u.TwoFactorChallengeTokenHash == tokenHash, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(u => u.Email.Value == email, cancellationToken);

    public void Add(User aggregate) => dbContext.Users.Add(aggregate);

    public void Update(User aggregate) => dbContext.Users.Update(aggregate);

    public void Remove(User aggregate) => dbContext.Users.Remove(aggregate);
}
