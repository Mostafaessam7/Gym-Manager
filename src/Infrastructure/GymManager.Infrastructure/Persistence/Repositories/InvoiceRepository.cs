using GymManager.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class InvoiceRepository(GymManagerDbContext dbContext) : IInvoiceRepository
{
    // Lines is an owned collection mapped to its own table; Issue() checks Lines.Count and AddLine()
    // appends to it, so both would silently misbehave against an unloaded (empty) collection.
    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default) =>
        dbContext.Invoices.CountAsync(cancellationToken);

    public void Add(Invoice aggregate) => dbContext.Invoices.Add(aggregate);

    public void Update(Invoice aggregate) => dbContext.Invoices.Update(aggregate);

    public void Remove(Invoice aggregate) => dbContext.Invoices.Remove(aggregate);
}
