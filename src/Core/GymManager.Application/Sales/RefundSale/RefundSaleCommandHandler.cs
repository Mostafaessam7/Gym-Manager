using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Payments;
using GymManager.Domain.Products;
using GymManager.Domain.Sales;
using GymManager.Domain.Sales.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Sales.RefundSale;

public sealed class RefundSaleCommandHandler(
    ISaleRepository saleRepository, IProductRepository productRepository, IPaymentRepository paymentRepository,
    IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<RefundSaleCommand>
{
    public async Task<Result> Handle(RefundSaleCommand command, CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetByIdAsync(command.SaleId, cancellationToken);
        if (sale is null)
            return Result.Failure(SaleErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(sale.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        // Captured before Refund() zeroes out RemainingQuantity, so a line already partially refunded
        // earlier only has its outstanding quantity restocked here, not the full original quantity.
        var quantitiesToRestock = sale.Lines.Select(l => (l.ProductId, l.RemainingQuantity)).ToList();

        var result = sale.Refund();
        if (result.IsFailure)
            return result;

        foreach (var (productId, quantity) in quantitiesToRestock)
        {
            if (quantity <= 0)
                continue;

            var product = await productRepository.GetByIdAsync(productId, cancellationToken);
            product?.ReceiveStock(quantity);
            if (product is not null)
                productRepository.Update(product);
        }

        foreach (var salePayment in sale.Payments)
        {
            var payment = await paymentRepository.GetByIdAsync(salePayment.PaymentId, cancellationToken);
            if (payment is null || payment.Status == PaymentStatus.Refunded)
                continue;

            payment.Refund();
            paymentRepository.Update(payment);
        }

        saleRepository.Update(sale);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
