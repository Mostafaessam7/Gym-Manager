using GymManager.Domain.Sales.Errors;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;
using GymManager.Domain.Common;

namespace GymManager.Domain.Sales;

/// <summary>A single product line on a <see cref="Sale"/>, priced at the time of sale.</summary>
public sealed class SaleLine : Entity<Guid>
{
    private SaleLine()
    {
        ProductNameSnapshot = string.Empty;
        UnitPrice = null!;
    }

    internal SaleLine(Guid productId, string productNameSnapshot, int quantity, Money unitPrice) : base(Guid.NewGuid())
    {
        ProductId = productId;
        ProductNameSnapshot = productNameSnapshot;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid ProductId { get; private set; }

    public string ProductNameSnapshot { get; private set; }

    public int Quantity { get; private set; }

    public Money UnitPrice { get; private set; }

    public int RefundedQuantity { get; private set; }

    public Money LineTotal => Money.Create(UnitPrice.Amount * Quantity, UnitPrice.Currency).Value;

    public int RemainingQuantity => Quantity - RefundedQuantity;

    public Money RefundTotal => Money.Create(UnitPrice.Amount * RefundedQuantity, UnitPrice.Currency).Value;

    internal Result<Money> RefundQuantity(int quantity)
    {
        if (quantity <= 0 || quantity > RemainingQuantity)
            return Result.Failure<Money>(SaleErrors.RefundQuantityExceedsRemaining);

        RefundedQuantity += quantity;
        return Money.Create(UnitPrice.Amount * quantity, UnitPrice.Currency);
    }
}
