using GymManager.Application.Sales.Contracts;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Sales.CreateSale;

/// <summary>When <paramref name="SplitPayments"/> is null or empty, the sale is paid in full via
/// <paramref name="PaymentMethod"/> (the original, single-method behavior). When provided, it replaces
/// <paramref name="PaymentMethod"/> entirely — each entry becomes its own payment allocation and their
/// amounts must sum to the sale total.</summary>
public sealed record CreateSaleCommand(
    Guid BranchId,
    Guid? MemberId,
    IReadOnlyCollection<SaleLineRequest> Lines,
    PaymentMethod PaymentMethod,
    IReadOnlyCollection<SalePaymentRequest>? SplitPayments = null) : ICommand<Result<SaleResponse>>;
