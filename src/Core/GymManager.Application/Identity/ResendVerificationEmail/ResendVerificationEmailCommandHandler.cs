using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace GymManager.Application.Identity.ResendVerificationEmail;

/// <summary>Always reports success, whether the email belongs to a registered account, an already-verified
/// one, or nobody at all — same anti-enumeration rationale as password reset.</summary>
public sealed class ResendVerificationEmailCommandHandler(
    IUserRepository userRepository, IEmailSender emailSender, IClientUrlProvider clientUrlProvider,
    IUnitOfWork unitOfWork, ILogger<ResendVerificationEmailCommandHandler> logger)
    : ICommandHandler<ResendVerificationEmailCommand>
{
    public async Task<Result> Handle(ResendVerificationEmailCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || user.IsEmailVerified)
            return Result.Success();

        await EmailVerificationSender.IssueAndSendAsync(user, emailSender, clientUrlProvider, logger, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
