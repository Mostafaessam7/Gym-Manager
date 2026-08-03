using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Identity;

/// <summary>A single-use backup code that can complete a 2FA challenge in place of a TOTP code, for when the
/// user has lost access to their authenticator device. Only the hash is ever stored.</summary>
public sealed class TwoFactorRecoveryCode : Entity<Guid>
{
    private TwoFactorRecoveryCode()
    {
        CodeHash = string.Empty;
    }

    internal TwoFactorRecoveryCode(string codeHash) : base(Guid.NewGuid())
    {
        CodeHash = codeHash;
        CreatedOnUtc = DateTimeOffset.UtcNow;
    }

    public string CodeHash { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public bool IsUsed { get; private set; }

    public DateTimeOffset? UsedOnUtc { get; private set; }

    internal void MarkUsed()
    {
        IsUsed = true;
        UsedOnUtc = DateTimeOffset.UtcNow;
    }
}
