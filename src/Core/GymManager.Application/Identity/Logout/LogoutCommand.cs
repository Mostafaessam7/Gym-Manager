using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand;
