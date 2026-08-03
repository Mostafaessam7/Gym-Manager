using GymManager.Application.Abstractions;
using GymManager.Application.GiftCards.Contracts;
using GymManager.Domain.GiftCards.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.GiftCards.GetGiftCardByCode;

public sealed class GetGiftCardByCodeQueryHandler(IApplicationReadDb readDb)
    : IQueryHandler<GetGiftCardByCodeQuery, Result<GiftCardResponse>>
{
    public async Task<Result<GiftCardResponse>> Handle(GetGiftCardByCodeQuery query, CancellationToken cancellationToken)
    {
        var normalizedCode = query.Code.Trim().ToUpperInvariant();
        var giftCard = await readDb.GiftCards.FirstOrDefaultAsync(g => g.Code == normalizedCode, cancellationToken);
        if (giftCard is null)
            return Result.Failure<GiftCardResponse>(GiftCardErrors.NotFound);

        return Result.Success(giftCard.ToResponse());
    }
}
