using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Products;
using GymManager.Domain.Products.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Products.ReceiveStock;

public sealed class ReceiveStockCommandHandler(
    IProductRepository productRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<ReceiveStockCommand>
{
    public async Task<Result> Handle(ReceiveStockCommand command, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return Result.Failure(ProductErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(product.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var result = product.ReceiveStock(command.Quantity);
        if (result.IsFailure)
            return result;

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
