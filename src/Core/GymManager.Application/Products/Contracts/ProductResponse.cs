using GymManager.Domain.Products;

namespace GymManager.Application.Products.Contracts;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    string Sku,
    string Category,
    decimal Price,
    decimal CostPrice,
    string Currency,
    Guid BranchId,
    int StockQuantity,
    int ReorderThreshold,
    bool IsLowStock,
    bool IsActive);

public static class ProductMappingExtensions
{
    public static ProductResponse ToResponse(this Product product) => new(
        product.Id, product.Name, product.Description, product.Sku, product.Category.ToString(),
        product.Price.Amount, product.CostPrice.Amount, product.Price.Currency, product.BranchId,
        product.StockQuantity, product.ReorderThreshold, product.IsLowStock, product.IsActive);
}
