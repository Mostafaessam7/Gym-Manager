using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.Users.RevokeRole;

public sealed record RevokeRoleCommand(Guid UserId, Guid RoleId) : ICommand;
