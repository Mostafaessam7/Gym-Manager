using GymManager.Application.Abstractions;
using GymManager.Application.Identity;
using GymManager.Application.Identity.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Identity;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace GymManager.Application.Identity.Register;

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IEmailSender emailSender,
    IClientUrlProvider clientUrlProvider,
    IUnitOfWork unitOfWork,
    ILogger<RegisterUserCommandHandler> logger)
    : ICommandHandler<RegisterUserCommand, Result<AuthenticationResponse>>
{
    public async Task<Result<AuthenticationResponse>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
            return Result.Failure<AuthenticationResponse>(emailResult.Error);

        if (await userRepository.EmailExistsAsync(emailResult.Value.Value, cancellationToken))
            return Result.Failure<AuthenticationResponse>(UserErrors.EmailAlreadyInUse(emailResult.Value.Value));

        var passwordHash = passwordHasher.Hash(command.Password);

        var user = User.Register(emailResult.Value, passwordHash, command.FirstName, command.LastName, command.BranchId);

        userRepository.Add(user);

        var accessToken = jwtTokenService.GenerateAccessToken(user, []);
        var refreshToken = jwtTokenService.GenerateRefreshToken();
        var refreshTokenExpiration = jwtTokenService.GetRefreshTokenExpiration();

        user.IssueRefreshToken(SecureTokenHasher.Hash(refreshToken), refreshTokenExpiration);

        await EmailVerificationSender.IssueAndSendAsync(user, emailSender, clientUrlProvider, logger, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthenticationResponse(
            user.Id,
            user.Email.Value,
            accessToken,
            jwtTokenService.GetAccessTokenExpiration(),
            refreshToken,
            refreshTokenExpiration,
            [],
            []));
    }
}
