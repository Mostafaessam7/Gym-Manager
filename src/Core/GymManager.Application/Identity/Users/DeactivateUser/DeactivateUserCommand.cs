using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.Users.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId) : ICommand;
