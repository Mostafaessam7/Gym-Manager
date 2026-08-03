using GymManager.Domain.Lockers;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class LockerRepository(GymManagerDbContext dbContext) : ILockerRepository
{
    public Task<Locker?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Lockers.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<bool> NumberExistsAsync(Guid branchId, string number, CancellationToken cancellationToken = default) =>
        dbContext.Lockers.AnyAsync(l => l.BranchId == branchId && l.Number == number, cancellationToken);

    public void Add(Locker aggregate) => dbContext.Lockers.Add(aggregate);

    public void Update(Locker aggregate) => dbContext.Lockers.Update(aggregate);

    public void Remove(Locker aggregate) => dbContext.Lockers.Remove(aggregate);
}
