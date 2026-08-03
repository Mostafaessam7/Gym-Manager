using GymManager.Application.Abstractions;
using GymManager.Application.Identity.Contracts;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Identity.Roles.GetRoles;

public sealed class GetRolesQueryHandler(IApplicationReadDb readDb) : IQueryHandler<GetRolesQuery, IReadOnlyList<RoleResponse>>
{
    public async Task<IReadOnlyList<RoleResponse>> Handle(GetRolesQuery query, CancellationToken cancellationToken)
    {
        var roles = await readDb.Roles.OrderBy(r => r.Name).ToListAsync(cancellationToken);

        return roles
            .Select(r => new RoleResponse(r.Id, r.Name, r.Description, r.IsSystemRole, r.Permissions.Select(p => p.Code).ToArray()))
            .ToList();
    }
}
