using GymManager.Domain.Common;
using GymManager.Domain.GiftCards;
using Xunit;

namespace GymManager.UnitTests.GiftCards;

public sealed class GiftCardTests
{
    private static GiftCard CreateGiftCard(decimal balance = 100m) =>
        GiftCard.Issue("GC-TEST1234", Money.Create(balance).Value, issuedToMemberId: null, expiresOnUtc: null);

    [Fact]
    public void Issue_Should_Set_Balances_And_Record_An_Issued_Transaction()
    {
        var giftCard = CreateGiftCard(100m);

        Assert.Equal(100m, giftCard.InitialBalance.Amount);
        Assert.Equal(100m, giftCard.CurrentBalance.Amount);
        Assert.True(giftCard.IsActive);
        Assert.Single(giftCard.Transactions);
        Assert.Equal(GiftCardTransactionType.Issued, giftCard.Transactions.Single().Type);
    }

    [Fact]
    public void Redeem_Should_Reduce_The_Current_Balance()
    {
        var giftCard = CreateGiftCard(100m);

        var result = giftCard.Redeem(Money.Create(30m).Value, referenceSaleId: Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(70m, giftCard.CurrentBalance.Amount);
        Assert.Equal(2, giftCard.Transactions.Count);
    }

    [Fact]
    public void Redeem_More_Than_The_Balance_Should_Fail()
    {
        var giftCard = CreateGiftCard(20m);

        var result = giftCard.Redeem(Money.Create(30m).Value, null, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("GiftCard.InsufficientBalance", result.Error.Code);
        Assert.Equal(20m, giftCard.CurrentBalance.Amount);
    }

    [Fact]
    public void Redeem_On_A_Deactivated_Card_Should_Fail()
    {
        var giftCard = CreateGiftCard(100m);
        giftCard.Deactivate();

        var result = giftCard.Redeem(Money.Create(10m).Value, null, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("GiftCard.Inactive", result.Error.Code);
    }

    [Fact]
    public void Redeem_On_An_Expired_Card_Should_Fail()
    {
        var giftCard = GiftCard.Issue("GC-EXPIRED1", Money.Create(100m).Value, null, DateTimeOffset.UtcNow.AddDays(-1));

        var result = giftCard.Redeem(Money.Create(10m).Value, null, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("GiftCard.Expired", result.Error.Code);
    }

    [Fact]
    public void Redeem_A_Zero_Amount_Should_Fail()
    {
        var giftCard = CreateGiftCard(100m);

        var result = giftCard.Redeem(Money.Zero(), null, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("GiftCard.InvalidAmount", result.Error.Code);
    }

    [Fact]
    public void Reload_Should_Increase_The_Current_Balance()
    {
        var giftCard = CreateGiftCard(50m);

        var result = giftCard.Reload(Money.Create(25m).Value);

        Assert.True(result.IsSuccess);
        Assert.Equal(75m, giftCard.CurrentBalance.Amount);
        Assert.Equal(50m, giftCard.InitialBalance.Amount);
    }

    [Fact]
    public void Deactivate_Then_Reactivate_Should_Toggle_IsActive()
    {
        var giftCard = CreateGiftCard();

        giftCard.Deactivate();
        Assert.False(giftCard.IsActive);

        giftCard.Reactivate();
        Assert.True(giftCard.IsActive);
    }
}
