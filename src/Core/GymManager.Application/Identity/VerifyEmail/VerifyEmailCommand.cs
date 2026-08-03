using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.VerifyEmail;

public sealed record VerifyEmailCommand(string Token) : ICommand;
