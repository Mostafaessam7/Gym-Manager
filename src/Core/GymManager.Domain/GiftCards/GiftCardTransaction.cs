using GymManager.Domain.Common;
using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.GiftCards;

/// <summary>An entry in a gift card's balance history — issuance, redemption against a sale, or a reload.</summary>
public sealed class GiftCardTransaction : Entity<Guid>
{
    private GiftCardTransaction()
    {
        Amount = null!;
    }

    internal GiftCardTransaction(GiftCardTransactionType type, Money amount, Guid? referenceSaleId, string? notes)
        : base(Guid.NewGuid())
    {
        Type = type;
        Amount = amount;
        ReferenceSaleId = referenceSaleId;
        Notes = notes;
        OccurredOnUtc = DateTimeOffset.UtcNow;
    }

    public GiftCardTransactionType Type { get; private set; }

    public Money Amount { get; private set; }

    /// <summary>The sale this redemption paid for, if <see cref="Type"/> is <see cref="GiftCardTransactionType.Redeemed"/>.</summary>
    public Guid? ReferenceSaleId { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset OccurredOnUtc { get; private set; }
}
