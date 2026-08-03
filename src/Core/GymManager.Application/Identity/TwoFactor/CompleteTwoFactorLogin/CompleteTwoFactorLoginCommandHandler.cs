using GymManager.Application.Abstractions;
using GymManager.Application.Identity;
using GymManager.Application.Identity.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.TwoFactor.CompleteTwoFactorLogin;

public sealed class CompleteTwoFactorLoginCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IJwtTokenService jwtTokenService,
    ITwoFactorService twoFactorService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CompleteTwoFactorLoginCommand, Result<AuthenticationResponse>>
{
    public async Task<Result<AuthenticationResponse>> Handle(CompleteTwoFactorLoginCommand command, CancellationToken cancellationToken)
    {
        var challengeTokenHash = SecureTokenHasher.Hash(command.ChallengeToken);
        var user = await userRepository.GetByTwoFactorChallengeTokenHashAsync(challengeTokenHash, cancellationToken);
        if (user is null)
            return Result.Failure<AuthenticationResponse>(UserErrors.TwoFactorChallengeRequired);

        var challengeResult = user.CompleteTwoFactorChallenge(challengeTokenHash, DateTimeOffset.UtcNow);
        if (challengeResult.IsFailure)
            return Result.Failure<AuthenticationResponse>(challengeResult.Error);

        var codeIsValid = user.TwoFactorSecretKey is not null && twoFactorService.ValidateCode(user.TwoFactorSecretKey, command.Code);
        if (!codeIsValid)
        {
            var recoveryResult = user.ConsumeTwoFactorRecoveryCode(SecureTokenHasher.Hash(command.Code));
            if (recoveryResult.IsFailure)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Failure<AuthenticationResponse>(UserErrors.TwoFactorCodeInvalid);
            }
        }

        var roles = await roleRepository.GetByIdsAsync(user.Roles.Select(r => r.RoleId), cancellationToken);
        var permissions = roles.SelectMany(r => r.Permissions.Select(p => p.Code)).Distinct().ToArray();

        user.RecordLogin();

        var accessToken = jwtTokenService.GenerateAccessToken(user, permissions);
        var refreshToken = jwtTokenService.GenerateRefreshToken();
        var refreshTokenExpiration = jwtTokenService.GetRefreshTokenExpiration();

        user.IssueRefreshToken(SecureTokenHasher.Hash(refreshToken), refreshTokenExpiration, command.IpAddress, command.UserAgent);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthenticationResponse(
            user.Id,
            user.Email.Value,
            accessToken,
            jwtTokenService.GetAccessTokenExpiration(),
            refreshToken,
            refreshTokenExpiration,
            roles.Select(r => r.Name).ToArray(),
            permissions));
    }
}
