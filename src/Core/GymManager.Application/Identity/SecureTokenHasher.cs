using System.Security.Cryptography;
using System.Text;

namespace GymManager.Application.Identity;

/// <summary>Generates and hashes high-entropy, single-use tokens: password-reset links, email-verification
/// links, 2FA challenge tokens, and 2FA recovery codes. A plain SHA-256 hash (not the slow, salted
/// <c>IPasswordHasher</c> used for account passwords) is deliberate here: these tokens are random values the
/// system generates, not user-chosen, so they need collision resistance for a lookup-by-hash query, not
/// resistance to offline brute-forcing of a small keyspace.</summary>
public static class SecureTokenHasher
{
    public static string GenerateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
