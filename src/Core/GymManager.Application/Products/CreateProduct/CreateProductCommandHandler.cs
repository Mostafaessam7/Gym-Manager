using GymManager.Application.Abstractions;
using GymManager.Application.Products.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Products;
using GymManager.Domain.Products.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Products.CreateProduct;

public sealed class CreateProductCommandHandler(
    IProductRepository productRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CreateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<ProductResponse>(accessResult.Error);

        var sku = command.Sku.Trim().ToUpperInvariant();
        if (await productRepository.SkuExistsAsync(sku, cancellationToken))
            return Result.Failure<ProductResponse>(ProductErrors.SkuAlreadyInUse(sku));

        var priceResult = Money.Create(command.Price, command.Currency);
        if (priceResult.IsFailure)
            return Result.Failure<ProductResponse>(priceResult.Error);

        var costResult = Money.Create(command.CostPrice, command.Currency);
        if (costResult.IsFailure)
            return Result.Failure<ProductResponse>(costResult.Error);

        var product = Product.Create(
            command.Name, command.Description, sku, command.Category, priceResult.Value, costResult.Value,
            command.BranchId, command.InitialStock, command.ReorderThreshold);

        productRepository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(product.ToResponse());
    }
}
