using GymManager.Application.Abstractions;
using GymManager.Domain.Identity;

namespace GymManager.Application.Identity;

/// <summary>Checks a candidate new password against a user's current password and their recent history.
/// This has to live in the Application layer rather than on the <see cref="User"/> aggregate itself, because
/// checking requires <see cref="IPasswordHasher.Verify"/> against plaintext — the domain layer only ever
/// sees hashes, by design.</summary>
public static class PasswordHistoryPolicy
{
    public static bool IsReuseOfRecentPassword(User user, string candidatePlainTextPassword, IPasswordHasher passwordHasher)
    {
        if (passwordHasher.Verify(candidatePlainTextPassword, user.PasswordHash))
            return true;

        return user.PasswordHistory.Any(entry => passwordHasher.Verify(candidatePlainTextPassword, entry.PasswordHash));
    }
}
