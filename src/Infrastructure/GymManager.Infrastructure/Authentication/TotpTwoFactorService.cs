using System.Security.Cryptography;
using System.Text;
using GymManager.Application.Abstractions;

namespace GymManager.Infrastructure.Authentication;

/// <summary>
/// Hand-rolled RFC 6238 TOTP (the algorithm behind Google/Microsoft Authenticator) plus RFC 4226 HOTP counter
/// arithmetic, using <see cref="HMACSHA1"/> — the algorithm every mainstream authenticator app assumes when it
/// scans a bare <c>otpauth://</c> URI with no explicit <c>algorithm</c> parameter. No third-party OTP package
/// is used; this mirrors the same "hash it ourselves with a documented RFC" approach already taken for
/// <see cref="PasswordHasher"/> and <c>SecureTokenHasher</c> elsewhere in this codebase.
/// </summary>
public sealed class TotpTwoFactorService(string issuer) : ITwoFactorService
{
    private const int SecretByteLength = 20;
    private const int CodeDigits = 6;
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(30);
    private const int AllowedDriftSteps = 1;

    public string GenerateSecretKey() => Base32.Encode(RandomNumberGenerator.GetBytes(SecretByteLength));

    public string GenerateProvisioningUri(string accountEmail, string secretKey)
    {
        var label = Uri.EscapeDataString($"{issuer}:{accountEmail}");
        var encodedIssuer = Uri.EscapeDataString(issuer);
        return $"otpauth://totp/{label}?secret={secretKey}&issuer={encodedIssuer}&digits={CodeDigits}&period={(int)Step.TotalSeconds}";
    }

    public bool ValidateCode(string secretKey, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != CodeDigits || !code.All(char.IsDigit))
            return false;

        var secretBytes = Base32.Decode(secretKey);
        var currentStep = GetCurrentTimeStep();

        for (var drift = -AllowedDriftSteps; drift <= AllowedDriftSteps; drift++)
        {
            var candidate = ComputeCode(secretBytes, currentStep + drift);
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(candidate), Encoding.ASCII.GetBytes(code)))
                return true;
        }

        return false;
    }

    public IReadOnlyCollection<string> GenerateRecoveryCodes(int count = 8)
    {
        var codes = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var bytes = RandomNumberGenerator.GetBytes(5);
            codes.Add(string.Join('-', Convert.ToHexString(bytes).Chunk(4).Select(chunk => new string(chunk))));
        }

        return codes;
    }

    private static long GetCurrentTimeStep() =>
        (long)(DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch).TotalSeconds / (long)Step.TotalSeconds;

    private static string ComputeCode(byte[] secretBytes, long timeStep)
    {
        var counterBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0F;
        var binaryCode =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        var code = binaryCode % (int)Math.Pow(10, CodeDigits);
        return code.ToString().PadLeft(CodeDigits, '0');
    }
}

/// <summary>Minimal RFC 4648 Base32 codec — just enough for TOTP secret keys (uppercase alphabet, no padding
/// required on decode since authenticator apps commonly omit it).</summary>
internal static class Base32
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Encode(byte[] data)
    {
        var builder = new StringBuilder((data.Length * 8 + 4) / 5);
        var bitBuffer = 0;
        var bitsInBuffer = 0;

        foreach (var b in data)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitsInBuffer += 8;

            while (bitsInBuffer >= 5)
            {
                bitsInBuffer -= 5;
                builder.Append(Alphabet[(bitBuffer >> bitsInBuffer) & 0x1F]);
            }
        }

        if (bitsInBuffer > 0)
            builder.Append(Alphabet[(bitBuffer << (5 - bitsInBuffer)) & 0x1F]);

        return builder.ToString();
    }

    public static byte[] Decode(string base32)
    {
        var cleaned = base32.Trim().TrimEnd('=').ToUpperInvariant();
        var bytes = new List<byte>((cleaned.Length * 5) / 8);
        var bitBuffer = 0;
        var bitsInBuffer = 0;

        foreach (var c in cleaned)
        {
            var index = Alphabet.IndexOf(c);
            if (index < 0)
                continue;

            bitBuffer = (bitBuffer << 5) | index;
            bitsInBuffer += 5;

            if (bitsInBuffer >= 8)
            {
                bitsInBuffer -= 8;
                bytes.Add((byte)((bitBuffer >> bitsInBuffer) & 0xFF));
            }
        }

        return [.. bytes];
    }
}
