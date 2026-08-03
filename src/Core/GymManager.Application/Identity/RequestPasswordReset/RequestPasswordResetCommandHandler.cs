using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace GymManager.Application.Identity.RequestPasswordReset;

/// <summary>Issues a password-reset token and emails it, if — and only if — the address belongs to a
/// registered account. The command always reports success either way (an unknown email is silently
/// ignored) so the endpoint cannot be used to enumerate registered accounts.</summary>
public sealed class RequestPasswordResetCommandHandler(
    IUserRepository userRepository, IEmailSender emailSender, IUnitOfWork unitOfWork, IClientUrlProvider clientUrlProvider,
    ILogger<RequestPasswordResetCommandHandler> logger)
    : ICommandHandler<RequestPasswordResetCommand>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task<Result> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null)
            return Result.Success();

        var token = SecureTokenHasher.GenerateToken();
        user.SetPasswordResetToken(SecureTokenHasher.Hash(token), DateTimeOffset.UtcNow.Add(TokenLifetime));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var resetLink = $"{clientUrlProvider.BaseUrl}/reset-password.html?token={token}&email={Uri.EscapeDataString(user.Email.Value)}";
        var body = $"""
            <p>We received a request to reset your Gym Manager password.</p>
            <p><a href="{resetLink}">Click here to choose a new password</a>. This link expires in 1 hour.</p>
            <p>If you didn't request this, you can safely ignore this email.</p>
            """;

        try
        {
            await emailSender.SendAsync(user.Email.Value, "Reset your Gym Manager password", body, cancellationToken);
        }
        catch (Exception exception)
        {
            // The token is already persisted — a delivery failure shouldn't turn an otherwise-successful
            // request into a 500, and reporting failure here would leak account existence anyway.
            logger.LogError(exception, "Failed to send password reset email to {Email}", user.Email.Value);
        }

        return Result.Success();
    }
}
