using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Products;

public interface IProductRepository : IRepository<Product, Guid>
{
    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default);
}
