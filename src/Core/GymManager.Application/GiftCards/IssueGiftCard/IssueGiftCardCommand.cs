using GymManager.Application.GiftCards.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.GiftCards.IssueGiftCard;

/// <summary>Issues a new gift card. <paramref name="Code"/> is optional — omit it to have one generated.</summary>
public sealed record IssueGiftCardCommand(decimal InitialBalance, string? Code, Guid? IssuedToMemberId, DateTimeOffset? ExpiresOnUtc)
    : ICommand<Result<GiftCardResponse>>;
