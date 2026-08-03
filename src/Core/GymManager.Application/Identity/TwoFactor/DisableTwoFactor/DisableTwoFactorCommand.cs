using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.TwoFactor.DisableTwoFactor;

public sealed record DisableTwoFactorCommand(Guid UserId, string CurrentPassword) : ICommand;
