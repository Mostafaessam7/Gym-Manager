using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.Users.AssignRole;

public sealed record AssignRoleCommand(Guid UserId, Guid RoleId) : ICommand;
