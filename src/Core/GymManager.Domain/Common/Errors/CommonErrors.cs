using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Common.Errors;

/// <summary>Catalog of expected failures for shared value objects used across bounded contexts.</summary>
public static class CommonErrors
{
    public static readonly Error EmailEmpty = Error.Validation("Email.Empty", "Email address is required.");

    public static readonly Error EmailInvalid = Error.Validation("Email.Invalid", "Email address is not a valid format.");

    public static readonly Error MoneyNegative = Error.Validation("Money.Negative", "Amount cannot be negative.");

    public static readonly Error PhoneNumberInvalid = Error.Validation("PhoneNumber.Invalid", "Phone number is not a valid format.");
}
