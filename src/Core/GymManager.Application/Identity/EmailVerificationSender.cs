using GymManager.Application.Abstractions;
using GymManager.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace GymManager.Application.Identity;

/// <summary>Issues a fresh email-verification token for a user and emails the verification link. Shared by
/// both registration (first send) and the resend-verification endpoint, so the token/email logic only exists
/// once.</summary>
public static class EmailVerificationSender
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    public static async Task IssueAndSendAsync(
        User user, IEmailSender emailSender, IClientUrlProvider clientUrlProvider, ILogger logger, CancellationToken cancellationToken)
    {
        var token = SecureTokenHasher.GenerateToken();
        user.SetEmailVerificationToken(SecureTokenHasher.Hash(token), DateTimeOffset.UtcNow.Add(TokenLifetime));

        var verifyLink = $"{clientUrlProvider.BaseUrl}/verify-email.html?token={token}";
        var body = $"""
            <p>Welcome to Gym Manager! Please confirm your email address to finish setting up your account.</p>
            <p><a href="{verifyLink}">Click here to verify your email</a>. This link expires in 24 hours.</p>
            """;

        try
        {
            await emailSender.SendAsync(user.Email.Value, "Verify your Gym Manager email address", body, cancellationToken);
        }
        catch (Exception exception)
        {
            // The token is already persisted (the caller still needs to SaveChanges) — a delivery failure
            // shouldn't turn an otherwise-successful registration/resend into a 500.
            logger.LogError(exception, "Failed to send verification email to {Email}", user.Email.Value);
        }
    }
}
