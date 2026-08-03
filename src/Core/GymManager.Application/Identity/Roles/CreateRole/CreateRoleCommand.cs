using GymManager.Application.Identity.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Identity.Roles.CreateRole;

public sealed record CreateRoleCommand(string Name, string Description, IReadOnlyCollection<string> Permissions)
    : ICommand<Result<RoleResponse>>;
