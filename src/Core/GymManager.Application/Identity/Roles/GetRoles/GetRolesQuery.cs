using GymManager.Application.Identity.Contracts;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.Roles.GetRoles;

public sealed record GetRolesQuery : IQuery<IReadOnlyList<RoleResponse>>;
