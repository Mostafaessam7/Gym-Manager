using GymManager.Infrastructure.Authentication;
using Xunit;

namespace GymManager.UnitTests.Authentication;

public sealed class TotpTwoFactorServiceTests
{
    private readonly TotpTwoFactorService _sut = new("GymManager");

    [Fact]
    public void GenerateSecretKey_Should_Return_A_Non_Empty_Base32_String()
    {
        var secret = _sut.GenerateSecretKey();

        Assert.False(string.IsNullOrWhiteSpace(secret));
        Assert.All(secret, c => Assert.Contains(c, "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"));
    }

    [Fact]
    public void GenerateSecretKey_Should_Return_A_Different_Secret_Each_Call()
    {
        Assert.NotEqual(_sut.GenerateSecretKey(), _sut.GenerateSecretKey());
    }

    [Fact]
    public void GenerateProvisioningUri_Should_Embed_The_Secret_Email_And_Issuer()
    {
        var uri = _sut.GenerateProvisioningUri("trainer@gym.io", "JBSWY3DPEHPK3PXP");

        Assert.StartsWith("otpauth://totp/", uri);
        Assert.Contains("secret=JBSWY3DPEHPK3PXP", uri);
        Assert.Contains("issuer=GymManager", uri);
    }

    [Fact]
    public void ValidateCode_Should_Accept_A_Code_Generated_For_The_Current_Time_Step()
    {
        var secret = _sut.GenerateSecretKey();
        var validCode = GenerateCodeForTesting(secret, DateTimeOffset.UtcNow);

        Assert.True(_sut.ValidateCode(secret, validCode));
    }

    [Fact]
    public void ValidateCode_Should_Reject_A_Code_From_A_Different_Secret()
    {
        var secretA = _sut.GenerateSecretKey();
        var secretB = _sut.GenerateSecretKey();
        var codeForB = GenerateCodeForTesting(secretB, DateTimeOffset.UtcNow);

        Assert.False(_sut.ValidateCode(secretA, codeForB));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    [InlineData("")]
    public void ValidateCode_Should_Reject_Malformed_Codes(string code)
    {
        var secret = _sut.GenerateSecretKey();

        Assert.False(_sut.ValidateCode(secret, code));
    }

    [Fact]
    public void GenerateRecoveryCodes_Should_Return_The_Requested_Count_Of_Unique_Codes()
    {
        var codes = _sut.GenerateRecoveryCodes(8);

        Assert.Equal(8, codes.Count);
        Assert.Equal(8, codes.Distinct().Count());
    }

    /// <summary>Regenerates a code the same way <see cref="TotpTwoFactorService"/> does internally, purely to
    /// give this test suite a known-good code to validate against without depending on a third-party OTP
    /// library that might compute it differently.</summary>
    private static string GenerateCodeForTesting(string secretKey, DateTimeOffset atTime)
    {
        var secretBytes = Base32DecodeForTesting(secretKey);
        var timeStep = (long)(atTime - DateTimeOffset.UnixEpoch).TotalSeconds / 30;

        var counterBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        using var hmac = new System.Security.Cryptography.HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0F;
        var binaryCode =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        return (binaryCode % 1_000_000).ToString().PadLeft(6, '0');
    }

    private static byte[] Base32DecodeForTesting(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        var bitBuffer = 0;
        var bitsInBuffer = 0;

        foreach (var c in base32.Trim().TrimEnd('=').ToUpperInvariant())
        {
            var index = alphabet.IndexOf(c);
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
