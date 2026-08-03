using GymManager.Application.Identity.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.TwoFactor.CompleteTwoFactorLogin;

/// <summary><paramref name="Code"/> may be either a 6-digit TOTP code or a one-time recovery code — the
/// handler tries the TOTP check first and falls back to recovery-code consumption.</summary>
public sealed record CompleteTwoFactorLoginCommand(
    string ChallengeToken, string Code, string? IpAddress = null, string? UserAgent = null)
    : ICommand<Result<AuthenticationResponse>>;
