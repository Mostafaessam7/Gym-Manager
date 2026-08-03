using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Products;
using GymManager.Domain.Sales;
using GymManager.Domain.Sales.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Sales.RefundSaleLine;

/// <summary>Restocks the returned quantity and marks it refunded on the sale line. Deliberately does not
/// attempt to auto-reverse a specific slice of a (possibly split) payment — allocating a partial refund
/// across several payment methods correctly needs a policy decision (refund to original method? to the
/// first? to store credit?) that belongs in a dedicated payments/refunds design, not folded into this
/// endpoint. The returned amount is reported back so staff can process it through whatever mechanism that
/// policy settles on.</summary>
public sealed class RefundSaleLineCommandHandler(
    ISaleRepository saleRepository, IProductRepository productRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<RefundSaleLineCommand, Result<decimal>>
{
    public async Task<Result<decimal>> Handle(RefundSaleLineCommand command, CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetByIdAsync(command.SaleId, cancellationToken);
        if (sale is null)
            return Result.Failure<decimal>(SaleErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(sale.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<decimal>(accessResult.Error);

        var line = sale.Lines.FirstOrDefault(l => l.Id == command.LineId);
        if (line is null)
            return Result.Failure<decimal>(SaleErrors.LineNotFound);

        var productId = line.ProductId;

        var refundResult = sale.RefundLine(command.LineId, command.Quantity);
        if (refundResult.IsFailure)
            return Result.Failure<decimal>(refundResult.Error);

        var product = await productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is not null)
        {
            product.ReceiveStock(command.Quantity);
            productRepository.Update(product);
        }

        saleRepository.Update(sale);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(refundResult.Value.Amount);
    }
}
