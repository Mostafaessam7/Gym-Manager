namespace GymManager.Application.Abstractions;

/// <summary>Generates and validates RFC 6238 TOTP codes (the "Google Authenticator" style 6-digit code that
/// rotates every 30 seconds) and the one-time recovery codes issued alongside them.</summary>
public interface ITwoFactorService
{
    /// <summary>Generates a random Base32-encoded shared secret for a new 2FA enrollment.</summary>
    string GenerateSecretKey();

    /// <summary>Builds the <c>otpauth://</c> provisioning URI an authenticator app scans as a QR code.</summary>
    string GenerateProvisioningUri(string accountEmail, string secretKey);

    /// <summary>Validates a 6-digit code against the secret, tolerating one 30-second step of clock drift in
    /// either direction.</summary>
    bool ValidateCode(string secretKey, string code);

    /// <summary>Generates a batch of high-entropy, human-typeable one-time recovery codes.</summary>
    IReadOnlyCollection<string> GenerateRecoveryCodes(int count = 8);
}
