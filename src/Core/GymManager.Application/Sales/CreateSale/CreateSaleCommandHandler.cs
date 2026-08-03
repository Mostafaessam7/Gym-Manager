using GymManager.Application.Abstractions;
using GymManager.Application.Sales.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.GiftCards;
using GymManager.Domain.GiftCards.Errors;
using GymManager.Domain.Payments;
using GymManager.Domain.Products;
using GymManager.Domain.Products.Errors;
using GymManager.Domain.Sales;
using GymManager.Domain.Sales.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Sales.CreateSale;

public sealed class CreateSaleCommandHandler(
    IProductRepository productRepository,
    ISaleRepository saleRepository,
    IPaymentRepository paymentRepository,
    IGiftCardRepository giftCardRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CreateSaleCommand, Result<SaleResponse>>
{
    public async Task<Result<SaleResponse>> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<SaleResponse>(accessResult.Error);

        var saleLines = new List<(Guid ProductId, string ProductName, int Quantity, Money UnitPrice)>();
        var products = new List<Product>();

        foreach (var line in command.Lines)
        {
            var product = await productRepository.GetByIdAsync(line.ProductId, cancellationToken);
            if (product is null)
                return Result.Failure<SaleResponse>(ProductErrors.NotFound);

            var deductResult = product.DeductStock(line.Quantity);
            if (deductResult.IsFailure)
                return Result.Failure<SaleResponse>(deductResult.Error);

            products.Add(product);
            saleLines.Add((product.Id, product.Name, line.Quantity, product.Price));
        }

        var saleResult = Sale.Create(command.BranchId, command.MemberId, currentUserService.UserId ?? Guid.Empty, saleLines);
        if (saleResult.IsFailure)
            return Result.Failure<SaleResponse>(saleResult.Error);

        var sale = saleResult.Value;

        if (command.SplitPayments is { Count: > 0 } splitPayments)
        {
            var allocatedTotal = splitPayments.Sum(p => p.Amount);
            if (allocatedTotal != sale.TotalAmount.Amount)
                return Result.Failure<SaleResponse>(SaleErrors.PaymentAmountMismatch);

            var giftCardsToUpdate = new List<GiftCard>();

            foreach (var allocation in splitPayments)
            {
                var amountResult = Money.Create(allocation.Amount, sale.TotalAmount.Currency);
                if (amountResult.IsFailure)
                    return Result.Failure<SaleResponse>(amountResult.Error);

                Guid? giftCardId = null;

                if (allocation.Method == PaymentMethod.GiftCard)
                {
                    var giftCard = await giftCardRepository.GetByCodeAsync(allocation.GiftCardCode!, cancellationToken);
                    if (giftCard is null)
                        return Result.Failure<SaleResponse>(GiftCardErrors.NotFound);

                    var redeemResult = giftCard.Redeem(amountResult.Value, sale.Id, DateTimeOffset.UtcNow);
                    if (redeemResult.IsFailure)
                        return Result.Failure<SaleResponse>(redeemResult.Error);

                    giftCardsToUpdate.Add(giftCard);
                    giftCardId = giftCard.Id;
                }

                var allocationPayment = Payment.Create(
                    command.MemberId ?? Guid.Empty, command.BranchId, amountResult.Value, allocation.Method,
                    PaymentReferenceType.ProductSale, sale.Id, currentUserService.UserId);
                allocationPayment.Complete();

                sale.AddPayment(allocation.Method, amountResult.Value, allocationPayment.Id, giftCardId);
                paymentRepository.Add(allocationPayment);
            }

            foreach (var giftCard in giftCardsToUpdate)
                giftCardRepository.Update(giftCard);
        }
        else
        {
            var payment = Payment.Create(
                command.MemberId ?? Guid.Empty, command.BranchId, sale.TotalAmount, command.PaymentMethod,
                PaymentReferenceType.ProductSale, sale.Id, currentUserService.UserId);
            payment.Complete();

            sale.AddPayment(command.PaymentMethod, sale.TotalAmount, payment.Id);
            paymentRepository.Add(payment);
        }

        foreach (var product in products)
            productRepository.Update(product);

        saleRepository.Add(sale);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(sale.ToResponse());
    }
}
