using GymManager.Domain.Branches;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class BranchRepository(GymManagerDbContext dbContext) : IBranchRepository
{
    public Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) =>
        dbContext.Branches.AnyAsync(b => b.Name == name, cancellationToken);

    public void Add(Branch aggregate) => dbContext.Branches.Add(aggregate);

    public void Update(Branch aggregate) => dbContext.Branches.Update(aggregate);

    public void Remove(Branch aggregate) => dbContext.Branches.Remove(aggregate);
}
