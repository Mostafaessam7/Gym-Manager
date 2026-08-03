using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Branches;

public interface IBranchRepository : IRepository<Branch, Guid>
{
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
}
