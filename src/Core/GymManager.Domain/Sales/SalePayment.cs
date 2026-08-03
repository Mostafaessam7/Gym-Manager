using GymManager.Domain.Common;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Sales;

/// <summary>One payment allocation on a <see cref="Sale"/> — a sale can be paid via a single method (most
/// common) or split across several (e.g. part cash, part card, part gift card).</summary>
public sealed class SalePayment : Entity<Guid>
{
    private SalePayment()
    {
        Amount = null!;
    }

    internal SalePayment(PaymentMethod method, Money amount, Guid paymentId, Guid? giftCardId)
        : base(Guid.NewGuid())
    {
        Method = method;
        Amount = amount;
        PaymentId = paymentId;
        GiftCardId = giftCardId;
    }

    public PaymentMethod Method { get; private set; }

    public Money Amount { get; private set; }

    /// <summary>The <c>Payment</c> aggregate recording this allocation's settlement.</summary>
    public Guid PaymentId { get; private set; }

    /// <summary>Set when <see cref="Method"/> is <see cref="PaymentMethod.GiftCard"/> — the card redeemed for
    /// this allocation.</summary>
    public Guid? GiftCardId { get; private set; }
}
