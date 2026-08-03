using GymManager.Application.Abstractions;
using GymManager.Application.Identity;
using GymManager.Application.Identity.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LoginCommand, Result<LoginResponse>>
{
    /// <summary>How long a caller has to complete the 2FA challenge before having to sign in again.</summary>
    private static readonly TimeSpan TwoFactorChallengeLifetime = TimeSpan.FromMinutes(5);

    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null)
            return Result.Failure<LoginResponse>(UserErrors.InvalidCredentials);

        var utcNow = DateTimeOffset.UtcNow;

        var lockoutCheck = user.VerifyIsNotLockedOut(utcNow);
        if (lockoutCheck.IsFailure)
            return Result.Failure<LoginResponse>(lockoutCheck.Error);

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            user.RecordFailedLoginAttempt(utcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<LoginResponse>(UserErrors.InvalidCredentials);
        }

        var activeCheck = user.VerifyIsActive();
        if (activeCheck.IsFailure)
            return Result.Failure<LoginResponse>(activeCheck.Error);

        if (user.TwoFactorEnabled)
        {
            var challengeToken = SecureTokenHasher.GenerateToken();
            user.IssueTwoFactorChallenge(SecureTokenHasher.Hash(challengeToken), utcNow.Add(TwoFactorChallengeLifetime));

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new LoginResponse(RequiresTwoFactor: true, challengeToken, Authentication: null));
        }

        var roles = await roleRepository.GetByIdsAsync(user.Roles.Select(r => r.RoleId), cancellationToken);
        var permissions = roles.SelectMany(r => r.Permissions.Select(p => p.Code)).Distinct().ToArray();

        user.RecordLogin();

        var accessToken = jwtTokenService.GenerateAccessToken(user, permissions);
        var refreshToken = jwtTokenService.GenerateRefreshToken();
        var refreshTokenExpiration = jwtTokenService.GetRefreshTokenExpiration();

        user.IssueRefreshToken(SecureTokenHasher.Hash(refreshToken), refreshTokenExpiration, command.IpAddress, command.UserAgent);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResponse(RequiresTwoFactor: false, TwoFactorChallengeToken: null, new AuthenticationResponse(
            user.Id,
            user.Email.Value,
            accessToken,
            jwtTokenService.GetAccessTokenExpiration(),
            refreshToken,
            refreshTokenExpiration,
            roles.Select(r => r.Name).ToArray(),
            permissions)));
    }
}
