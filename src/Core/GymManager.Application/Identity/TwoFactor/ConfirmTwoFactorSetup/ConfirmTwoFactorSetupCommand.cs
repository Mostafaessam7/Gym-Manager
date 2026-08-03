using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.TwoFactor.ConfirmTwoFactorSetup;

/// <summary>Completes 2FA enrollment by proving the caller's authenticator app can generate a valid code from
/// the pending secret. Returns the plaintext recovery codes — the only time they are ever visible.</summary>
public sealed record ConfirmTwoFactorSetupCommand(Guid UserId, string Code) : ICommand<Result<IReadOnlyCollection<string>>>;
