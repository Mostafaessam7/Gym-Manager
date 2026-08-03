using GymManager.Application.Identity;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.Logout;

/// <summary>Revokes the refresh token presented at sign-out. Always succeeds — an unknown or already-expired
/// token still results in the client being signed out, which is the caller's actual intent.</summary>
public sealed class LogoutCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var refreshTokenHash = SecureTokenHasher.Hash(command.RefreshToken);

        var user = await userRepository.GetByRefreshTokenHashAsync(refreshTokenHash, cancellationToken);
        if (user is null)
            return Result.Success();

        user.RevokeRefreshToken(refreshTokenHash);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
