using GymManager.Application.Identity.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.TwoFactor.StartTwoFactorSetup;

public sealed record StartTwoFactorSetupCommand(Guid UserId) : ICommand<Result<TwoFactorSetupResponse>>;
