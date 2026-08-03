using GymManager.Domain.Crm;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class LeadRepository(GymManagerDbContext dbContext) : ILeadRepository
{
    public Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Leads.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public void Add(Lead aggregate) => dbContext.Leads.Add(aggregate);

    public void Update(Lead aggregate) => dbContext.Leads.Update(aggregate);

    public void Remove(Lead aggregate) => dbContext.Leads.Remove(aggregate);
}
