using GymManager.Domain.Common.Errors;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Common;

/// <summary>An immutable monetary amount in a specific ISO currency code.</summary>
public sealed class Money : ValueObject
{
    public const string DefaultCurrency = "USD";

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Zero(string currency = DefaultCurrency) => new(0m, currency);

    public static Result<Money> Create(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0)
            return Result.Failure<Money>(CommonErrors.MoneyNegative);

        return Result.Success(new Money(amount, currency.Trim().ToUpperInvariant()));
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot combine amounts in {Currency} and {other.Currency}.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
