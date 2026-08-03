using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.RequestPasswordReset;

public sealed record RequestPasswordResetCommand(string Email) : ICommand;
