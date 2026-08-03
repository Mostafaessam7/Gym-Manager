using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
            return Result.Failure(UserErrors.InvalidCredentials);

        if (PasswordHistoryPolicy.IsReuseOfRecentPassword(user, command.NewPassword, passwordHasher))
            return Result.Failure(UserErrors.PasswordReusesRecentPassword);

        user.ChangePassword(passwordHasher.Hash(command.NewPassword), DateTimeOffset.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
