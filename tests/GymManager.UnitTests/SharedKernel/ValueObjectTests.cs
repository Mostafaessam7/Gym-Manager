using GymManager.SharedKernel.Primitives;
using Xunit;

namespace GymManager.UnitTests.SharedKernel;

public sealed class ValueObjectTests
{
    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        public decimal Amount { get; } = amount;

        public string Currency { get; } = currency;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void Two_Value_Objects_With_The_Same_Components_Should_Be_Equal()
    {
        var first = new Money(100m, "USD");
        var second = new Money(100m, "USD");

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_Value_Objects_With_Different_Components_Should_Not_Be_Equal()
    {
        var first = new Money(100m, "USD");
        var second = new Money(100m, "EUR");

        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }
}
