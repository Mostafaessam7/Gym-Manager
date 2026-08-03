using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : ICommand;
