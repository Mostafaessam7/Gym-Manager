using GymManager.Domain.Abstractions;
using GymManager.Domain.GiftCards;
using GymManager.Domain.GiftCards.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.GiftCards.DeactivateGiftCard;

public sealed class DeactivateGiftCardCommandHandler(IGiftCardRepository giftCardRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivateGiftCardCommand>
{
    public async Task<Result> Handle(DeactivateGiftCardCommand command, CancellationToken cancellationToken)
    {
        var giftCard = await giftCardRepository.GetByIdAsync(command.GiftCardId, cancellationToken);
        if (giftCard is null)
            return Result.Failure(GiftCardErrors.NotFound);

        giftCard.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
