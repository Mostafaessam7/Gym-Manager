using GymManager.Application.GiftCards.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.GiftCards.GetGiftCards;

public sealed record GetGiftCardsQuery(PaginationParameters Pagination, Guid? IssuedToMemberId, bool? IsActive)
    : IQuery<PagedList<GiftCardResponse>>;
