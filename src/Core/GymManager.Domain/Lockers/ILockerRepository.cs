using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Lockers;

public interface ILockerRepository : IRepository<Locker, Guid>
{
    Task<bool> NumberExistsAsync(Guid branchId, string number, CancellationToken cancellationToken = default);
}
