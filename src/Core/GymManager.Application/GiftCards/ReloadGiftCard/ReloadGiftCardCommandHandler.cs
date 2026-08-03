using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.GiftCards;
using GymManager.Domain.GiftCards.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.GiftCards.ReloadGiftCard;

public sealed class ReloadGiftCardCommandHandler(IGiftCardRepository giftCardRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<ReloadGiftCardCommand>
{
    public async Task<Result> Handle(ReloadGiftCardCommand command, CancellationToken cancellationToken)
    {
        var giftCard = await giftCardRepository.GetByIdAsync(command.GiftCardId, cancellationToken);
        if (giftCard is null)
            return Result.Failure(GiftCardErrors.NotFound);

        var amountResult = Money.Create(command.Amount, giftCard.CurrentBalance.Currency);
        if (amountResult.IsFailure)
            return Result.Failure(amountResult.Error);

        var result = giftCard.Reload(amountResult.Value);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
