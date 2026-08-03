using GymManager.Domain.GiftCards;

namespace GymManager.Application.GiftCards.Contracts;

public static class GiftCardMappingExtensions
{
    public static GiftCardResponse ToResponse(this GiftCard giftCard) => new(
        giftCard.Id,
        giftCard.Code,
        giftCard.InitialBalance.Amount,
        giftCard.CurrentBalance.Amount,
        giftCard.CurrentBalance.Currency,
        giftCard.IssuedToMemberId,
        giftCard.ExpiresOnUtc,
        giftCard.IsActive,
        [.. giftCard.Transactions
            .OrderByDescending(t => t.OccurredOnUtc)
            .Select(t => new GiftCardTransactionResponse(t.Id, t.Type.ToString(), t.Amount.Amount, t.ReferenceSaleId, t.Notes, t.OccurredOnUtc))],
        giftCard.CreatedOnUtc);
}
