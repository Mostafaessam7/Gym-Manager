using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.Roles.UpdateRolePermissions;

public sealed record UpdateRolePermissionsCommand(Guid RoleId, IReadOnlyCollection<string> Permissions) : ICommand;
