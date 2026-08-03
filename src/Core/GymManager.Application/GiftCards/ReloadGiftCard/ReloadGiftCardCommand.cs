using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.GiftCards.ReloadGiftCard;

public sealed record ReloadGiftCardCommand(Guid GiftCardId, decimal Amount) : ICommand;
