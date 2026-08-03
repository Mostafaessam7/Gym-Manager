using GymManager.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(GymManagerDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default) =>
        dbContext.Products.AnyAsync(p => p.Sku == sku, cancellationToken);

    public void Add(Product aggregate) => dbContext.Products.Add(aggregate);

    public void Update(Product aggregate) => dbContext.Products.Update(aggregate);

    public void Remove(Product aggregate) => dbContext.Products.Remove(aggregate);
}
