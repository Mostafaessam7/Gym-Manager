using GymManager.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository(GymManagerDbContext dbContext) : IRoleRepository
{
    // Permissions is an owned collection mapped to its own table; GrantPermission()/RevokePermission()
    // mutate it directly, so it must be loaded or the change tracker never observes the mutation.
    private IQueryable<Role> RolesWithPermissions => dbContext.Roles.Include(r => r.Permissions);

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        RolesWithPermissions.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        RolesWithPermissions.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        await RolesWithPermissions.Where(r => ids.Contains(r.Id)).ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) =>
        dbContext.Roles.AnyAsync(r => r.Name == name, cancellationToken);

    public void Add(Role aggregate) => dbContext.Roles.Add(aggregate);

    public void Update(Role aggregate) => dbContext.Roles.Update(aggregate);

    public void Remove(Role aggregate) => dbContext.Roles.Remove(aggregate);
}
