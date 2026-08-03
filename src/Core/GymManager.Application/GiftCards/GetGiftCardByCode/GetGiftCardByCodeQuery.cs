using GymManager.Application.GiftCards.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.GiftCards.GetGiftCardByCode;

public sealed record GetGiftCardByCodeQuery(string Code) : IQuery<Result<GiftCardResponse>>;
