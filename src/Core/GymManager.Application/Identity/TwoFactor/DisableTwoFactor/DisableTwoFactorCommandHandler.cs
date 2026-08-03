using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.TwoFactor.DisableTwoFactor;

/// <summary>Requires the current password (not just a valid access token) so a hijacked, still-logged-in
/// browser session alone can't turn off 2FA protection.</summary>
public sealed class DisableTwoFactorCommandHandler(
    IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    : ICommandHandler<DisableTwoFactorCommand>
{
    public async Task<Result> Handle(DisableTwoFactorCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
            return Result.Failure(UserErrors.InvalidCredentials);

        var result = user.DisableTwoFactor();
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
