using GymManager.Domain.Products;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId, string Name, string Description, ProductCategory Category, decimal Price, decimal CostPrice,
    string Currency, int ReorderThreshold) : ICommand;
