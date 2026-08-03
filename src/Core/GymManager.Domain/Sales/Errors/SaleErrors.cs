using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Sales.Errors;

public static class SaleErrors
{
    public static readonly Error NotFound = Error.NotFound("Sale.NotFound", "The sale was not found.");

    public static readonly Error NoLines = Error.Validation("Sale.NoLines", "A sale must have at least one line.");

    public static readonly Error AlreadyRefunded = Error.Conflict("Sale.AlreadyRefunded", "This sale has already been refunded.");

    public static readonly Error LineNotFound = Error.NotFound("Sale.LineNotFound", "The sale line was not found.");

    public static readonly Error RefundQuantityExceedsRemaining = Error.Validation(
        "Sale.RefundQuantityExceedsRemaining", "The refund quantity exceeds what remains unrefunded on this line.");

    public static readonly Error PaymentAmountMismatch = Error.Validation(
        "Sale.PaymentAmountMismatch", "The sum of the split payment amounts must equal the sale total.");
}
