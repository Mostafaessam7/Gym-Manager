using GymManager.Application.Abstractions;
using GymManager.Application.Identity;
using GymManager.Application.Identity.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.RefreshAccessToken;

public sealed class RefreshAccessTokenCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IJwtTokenService jwtTokenService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RefreshAccessTokenCommand, Result<AuthenticationResponse>>
{
    public async Task<Result<AuthenticationResponse>> Handle(RefreshAccessTokenCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByRefreshTokenHashAsync(SecureTokenHasher.Hash(command.RefreshToken), cancellationToken);
        if (user is null)
            return Result.Failure<AuthenticationResponse>(UserErrors.RefreshTokenInvalid);

        var activeCheck = user.VerifyIsActive();
        if (activeCheck.IsFailure)
            return Result.Failure<AuthenticationResponse>(activeCheck.Error);

        var newRefreshToken = jwtTokenService.GenerateRefreshToken();
        var newExpiration = jwtTokenService.GetRefreshTokenExpiration();

        var rotationResult = user.RotateRefreshToken(
            SecureTokenHasher.Hash(command.RefreshToken), SecureTokenHasher.Hash(newRefreshToken), newExpiration, command.IpAddress, command.UserAgent);
        if (rotationResult.IsFailure)
            return Result.Failure<AuthenticationResponse>(rotationResult.Error);

        var roles = await roleRepository.GetByIdsAsync(user.Roles.Select(r => r.RoleId), cancellationToken);
        var permissions = roles.SelectMany(r => r.Permissions.Select(p => p.Code)).Distinct().ToArray();

        var accessToken = jwtTokenService.GenerateAccessToken(user, permissions);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthenticationResponse(
            user.Id,
            user.Email.Value,
            accessToken,
            jwtTokenService.GetAccessTokenExpiration(),
            newRefreshToken,
            newExpiration,
            roles.Select(r => r.Name).ToArray(),
            permissions));
    }
}
