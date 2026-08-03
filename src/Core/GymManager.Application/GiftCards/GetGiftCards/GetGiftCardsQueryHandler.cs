using GymManager.Application.Abstractions;
using GymManager.Application.GiftCards.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.GiftCards.GetGiftCards;

public sealed class GetGiftCardsQueryHandler(IApplicationReadDb readDb) : IQueryHandler<GetGiftCardsQuery, PagedList<GiftCardResponse>>
{
    public async Task<PagedList<GiftCardResponse>> Handle(GetGiftCardsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var giftCards = readDb.GiftCards.AsQueryable();

        if (query.IssuedToMemberId.HasValue)
            giftCards = giftCards.Where(g => g.IssuedToMemberId == query.IssuedToMemberId);

        if (query.IsActive.HasValue)
            giftCards = giftCards.Where(g => g.IsActive == query.IsActive);

        var totalCount = await giftCards.CountAsync(cancellationToken);

        var page = await giftCards
            .OrderByDescending(g => g.CreatedOnUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var items = page.Select(g => g.ToResponse()).ToList();

        return new PagedList<GiftCardResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
