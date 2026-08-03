using GymManager.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class SaleRepository(GymManagerDbContext dbContext) : ISaleRepository
{
    // Lines is an owned collection mapped to its own table; RefundSale iterates it to restock products,
    // so it must be loaded explicitly or every refund would silently restock nothing.
    public Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Sales.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(Sale aggregate) => dbContext.Sales.Add(aggregate);

    public void Update(Sale aggregate) => dbContext.Sales.Update(aggregate);

    public void Remove(Sale aggregate) => dbContext.Sales.Remove(aggregate);
}
