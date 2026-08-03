using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = SecureTokenHasher.Hash(command.Token);

        var user = await userRepository.GetByPasswordResetTokenHashAsync(tokenHash, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.PasswordResetTokenInvalid);

        if (PasswordHistoryPolicy.IsReuseOfRecentPassword(user, command.NewPassword, passwordHasher))
            return Result.Failure(UserErrors.PasswordReusesRecentPassword);

        var result = user.ResetPassword(tokenHash, passwordHasher.Hash(command.NewPassword), DateTimeOffset.UtcNow);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
