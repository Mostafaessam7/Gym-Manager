using GymManager.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class CommissionRepository(GymManagerDbContext dbContext) : ICommissionRepository
{
    public Task<Commission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Commissions.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(Commission aggregate) => dbContext.Commissions.Add(aggregate);

    public void Update(Commission aggregate) => dbContext.Commissions.Update(aggregate);

    public void Remove(Commission aggregate) => dbContext.Commissions.Remove(aggregate);
}
