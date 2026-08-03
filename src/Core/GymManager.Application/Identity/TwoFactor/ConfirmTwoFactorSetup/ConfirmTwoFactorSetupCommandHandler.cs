using GymManager.Application.Abstractions;
using GymManager.Application.Identity;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.TwoFactor.ConfirmTwoFactorSetup;

public sealed class ConfirmTwoFactorSetupCommandHandler(
    IUserRepository userRepository, ITwoFactorService twoFactorService, IUnitOfWork unitOfWork)
    : ICommandHandler<ConfirmTwoFactorSetupCommand, Result<IReadOnlyCollection<string>>>
{
    public async Task<Result<IReadOnlyCollection<string>>> Handle(ConfirmTwoFactorSetupCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<IReadOnlyCollection<string>>(UserErrors.NotFound);

        if (user.TwoFactorSecretKey is null || !twoFactorService.ValidateCode(user.TwoFactorSecretKey, command.Code))
            return Result.Failure<IReadOnlyCollection<string>>(UserErrors.TwoFactorCodeInvalid);

        var recoveryCodes = twoFactorService.GenerateRecoveryCodes();
        var confirmResult = user.ConfirmTwoFactorSetup([.. recoveryCodes.Select(SecureTokenHasher.Hash)]);
        if (confirmResult.IsFailure)
            return Result.Failure<IReadOnlyCollection<string>>(confirmResult.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(recoveryCodes);
    }
}
