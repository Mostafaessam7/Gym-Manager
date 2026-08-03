namespace GymManager.Application.GiftCards.Contracts;

public sealed record GiftCardTransactionResponse(
    Guid Id, string Type, decimal Amount, Guid? ReferenceSaleId, string? Notes, DateTimeOffset OccurredOnUtc);

public sealed record GiftCardResponse(
    Guid Id,
    string Code,
    decimal InitialBalance,
    decimal CurrentBalance,
    string Currency,
    Guid? IssuedToMemberId,
    DateTimeOffset? ExpiresOnUtc,
    bool IsActive,
    IReadOnlyCollection<GiftCardTransactionResponse> Transactions,
    DateTimeOffset CreatedOnUtc);
