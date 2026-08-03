using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Products;
using GymManager.Domain.Products.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Products.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IProductRepository productRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpdateProductCommand>
{
    public async Task<Result> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return Result.Failure(ProductErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(product.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var priceResult = Money.Create(command.Price, command.Currency);
        if (priceResult.IsFailure)
            return priceResult;

        var costResult = Money.Create(command.CostPrice, command.Currency);
        if (costResult.IsFailure)
            return costResult;

        product.Update(command.Name, command.Description, command.Category, priceResult.Value, costResult.Value, command.ReorderThreshold);

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
