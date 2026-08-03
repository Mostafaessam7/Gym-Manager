using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.ResendVerificationEmail;

public sealed record ResendVerificationEmailCommand(string Email) : ICommand;
