using GymManager.Application.Abstractions;
using GymManager.Application.Identity.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.TwoFactor.StartTwoFactorSetup;

public sealed class StartTwoFactorSetupCommandHandler(
    IUserRepository userRepository, ITwoFactorService twoFactorService, IUnitOfWork unitOfWork)
    : ICommandHandler<StartTwoFactorSetupCommand, Result<TwoFactorSetupResponse>>
{
    public async Task<Result<TwoFactorSetupResponse>> Handle(StartTwoFactorSetupCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<TwoFactorSetupResponse>(UserErrors.NotFound);

        var secretKey = twoFactorService.GenerateSecretKey();
        var startResult = user.StartTwoFactorSetup(secretKey);
        if (startResult.IsFailure)
            return Result.Failure<TwoFactorSetupResponse>(startResult.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var provisioningUri = twoFactorService.GenerateProvisioningUri(user.Email.Value, secretKey);
        return Result.Success(new TwoFactorSetupResponse(secretKey, provisioningUri));
    }
}
