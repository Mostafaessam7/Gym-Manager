using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Identity;

/// <summary>An opaque, single-use rotation token issued alongside a JWT access token. Doubles as a "session"
/// record for session-management screens — IpAddress/UserAgent are captured purely for that display purpose
/// and never influence authentication decisions.
///
/// Only <see cref="TokenHash"/> (a SHA-256 hash, see <c>SecureTokenHasher</c> in the Application layer) is
/// ever persisted — never the raw token — the same way password-reset/email-verification/2FA tokens are
/// already handled, so a database read (backup leak, SQL injection, etc.) cannot yield directly-usable,
/// long-lived bearer-equivalent credentials.</summary>
public sealed class RefreshToken : Entity<Guid>
{
    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    internal RefreshToken(string tokenHash, DateTimeOffset expiresOnUtc, string? ipAddress = null, string? userAgent = null)
        : base(Guid.NewGuid())
    {
        TokenHash = tokenHash;
        ExpiresOnUtc = expiresOnUtc;
        CreatedOnUtc = DateTimeOffset.UtcNow;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresOnUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public DateTimeOffset? RevokedOnUtc { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public bool IsActive => RevokedOnUtc is null && DateTimeOffset.UtcNow < ExpiresOnUtc;

    internal void Revoke() => RevokedOnUtc = DateTimeOffset.UtcNow;
}
