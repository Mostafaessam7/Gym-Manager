using System.Security.Cryptography;
using System.Text;

namespace GymManager.SharedKernel.Primitives;

/// <summary>
/// Constant-time equality for secret values (token hashes, TOTP codes, recovery codes) — comparing them with
/// plain <c>==</c>/<c>string.Equals</c> leaks how many leading bytes matched via response timing. Shared by
/// <c>GymManager.Domain.Identity.User</c> (token/challenge-hash comparisons) and
/// <c>GymManager.Infrastructure.Authentication.TotpTwoFactorService</c> (TOTP code comparisons) so both use
/// one implementation instead of two independently-written ones.
/// </summary>
public static class ConstantTimeComparer
{
    /// <summary>True only if both values are non-null, the same length, and byte-for-byte equal — compared
    /// via <see cref="CryptographicOperations.FixedTimeEquals"/> so a mismatch's position never affects
    /// timing. A length mismatch short-circuits before that call: for the fixed-length hex hashes and
    /// digit codes this is used for, "wrong length" already means "not a value this system produced," so
    /// leaking that isn't a meaningful timing side-channel the way leaking *which byte* differs would be.</summary>
    public static bool Equals(string? a, string? b)
    {
        if (a is null || b is null)
            return false;

        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        return aBytes.Length == bBytes.Length && CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
