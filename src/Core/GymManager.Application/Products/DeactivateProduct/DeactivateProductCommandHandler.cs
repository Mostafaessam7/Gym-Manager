using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Products;
using GymManager.Domain.Products.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Products.DeactivateProduct;

public sealed class DeactivateProductCommandHandler(
    IProductRepository productRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<DeactivateProductCommand>
{
    public async Task<Result> Handle(DeactivateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return Result.Failure(ProductErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(product.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        product.Deactivate();

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
