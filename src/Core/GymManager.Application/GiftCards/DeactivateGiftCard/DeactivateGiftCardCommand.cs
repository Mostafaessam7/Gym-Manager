using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.GiftCards.DeactivateGiftCard;

public sealed record DeactivateGiftCardCommand(Guid GiftCardId) : ICommand;
