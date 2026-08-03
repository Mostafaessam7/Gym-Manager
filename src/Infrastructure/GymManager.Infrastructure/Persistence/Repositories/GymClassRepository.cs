using GymManager.Domain.Classes;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class GymClassRepository(GymManagerDbContext dbContext) : IGymClassRepository
{
    public Task<GymClass?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.GymClasses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) =>
        dbContext.GymClasses.AnyAsync(c => c.Name == name, cancellationToken);

    public void Add(GymClass aggregate) => dbContext.GymClasses.Add(aggregate);

    public void Update(GymClass aggregate) => dbContext.GymClasses.Update(aggregate);

    public void Remove(GymClass aggregate) => dbContext.GymClasses.Remove(aggregate);
}
