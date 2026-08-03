using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Identity;

/// <summary>A previously-used password hash, kept only long enough to stop immediate reuse (see
/// <see cref="User.PasswordHistoryLimit"/>). The plaintext password is never recoverable from this — it's
/// checked the same way a live password is, via <c>IPasswordHasher.Verify</c>.</summary>
public sealed class PasswordHistoryEntry : Entity<Guid>
{
    private PasswordHistoryEntry()
    {
        PasswordHash = string.Empty;
    }

    internal PasswordHistoryEntry(string passwordHash, DateTimeOffset createdOnUtc) : base(Guid.NewGuid())
    {
        PasswordHash = passwordHash;
        CreatedOnUtc = createdOnUtc;
    }

    public string PasswordHash { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
}
