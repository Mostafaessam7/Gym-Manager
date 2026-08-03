using GymManager.Application.Products.Contracts;
using GymManager.Domain.Products;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name, string Description, string Sku, ProductCategory Category, decimal Price, decimal CostPrice,
    string Currency, Guid BranchId, int InitialStock, int ReorderThreshold) : ICommand<Result<ProductResponse>>;
