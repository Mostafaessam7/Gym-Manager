using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Classes;

public interface IGymClassRepository : IRepository<GymClass, Guid>
{
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
}
