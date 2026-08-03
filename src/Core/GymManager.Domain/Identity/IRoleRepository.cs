using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Identity;

public interface IRoleRepository : IRepository<Role, Guid>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
}
