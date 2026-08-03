using GymManager.SharedKernel.Results;

namespace GymManager.Domain.GiftCards.Errors;

public static class GiftCardErrors
{
    public static readonly Error NotFound = Error.NotFound("GiftCard.NotFound", "The gift card was not found.");

    public static readonly Error CodeAlreadyInUse = Error.Conflict("GiftCard.CodeAlreadyInUse", "A gift card with this code already exists.");

    public static readonly Error Inactive = Error.Forbidden("GiftCard.Inactive", "This gift card has been deactivated.");

    public static readonly Error Expired = Error.Forbidden("GiftCard.Expired", "This gift card has expired.");

    public static readonly Error InsufficientBalance =
        Error.Validation("GiftCard.InsufficientBalance", "The gift card does not have enough remaining balance.");

    public static readonly Error InvalidAmount = Error.Validation("GiftCard.InvalidAmount", "The amount must be greater than zero.");
}
