using GymManager.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class SettingRepository(GymManagerDbContext dbContext) : ISettingRepository
{
    public Task<Setting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Settings.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Setting?> GetByKeyAsync(string key, Guid? branchId, CancellationToken cancellationToken = default) =>
        dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key && s.BranchId == branchId, cancellationToken);

    public Task<bool> KeyExistsAsync(string key, Guid? branchId, CancellationToken cancellationToken = default) =>
        dbContext.Settings.AnyAsync(s => s.Key == key && s.BranchId == branchId, cancellationToken);

    public void Add(Setting aggregate) => dbContext.Settings.Add(aggregate);

    public void Update(Setting aggregate) => dbContext.Settings.Update(aggregate);

    public void Remove(Setting aggregate) => dbContext.Settings.Remove(aggregate);
}
