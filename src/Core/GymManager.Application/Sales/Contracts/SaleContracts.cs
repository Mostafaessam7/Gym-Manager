using GymManager.Domain.Payments;

namespace GymManager.Application.Sales.Contracts;

public sealed record SaleLineRequest(Guid ProductId, int Quantity);

public sealed record SaleLineResponse(
    Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal,
    int RefundedQuantity, int RemainingQuantity);

/// <summary>One payment allocation to make against a sale. Provide more than one entry to split a sale
/// across several methods; a <see cref="Method"/> of <see cref="PaymentMethod.GiftCard"/> requires
/// <see cref="GiftCardCode"/>. Amounts must sum to the sale's total.</summary>
public sealed record SalePaymentRequest(PaymentMethod Method, decimal Amount, string? GiftCardCode);

public sealed record SalePaymentResponse(Guid Id, string Method, decimal Amount, Guid PaymentId, Guid? GiftCardId);

public sealed record SaleResponse(
    Guid Id,
    Guid BranchId,
    Guid? MemberId,
    Guid SoldByUserId,
    string Status,
    Guid? PaymentId,
    DateTimeOffset SoldOnUtc,
    decimal TotalAmount,
    string Currency,
    IReadOnlyCollection<SaleLineResponse> Lines,
    IReadOnlyCollection<SalePaymentResponse> Payments);
