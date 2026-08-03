using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.VerifyEmail;

public sealed class VerifyEmailCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<VerifyEmailCommand>
{
    public async Task<Result> Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = SecureTokenHasher.Hash(command.Token);

        var user = await userRepository.GetByEmailVerificationTokenHashAsync(tokenHash, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.EmailVerificationTokenInvalid);

        var result = user.VerifyEmail(tokenHash, DateTimeOffset.UtcNow);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
