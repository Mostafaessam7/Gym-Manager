using GymManager.Domain.Common;
using GymManager.Domain.Staff;
using Xunit;

namespace GymManager.UnitTests.Staff;

public sealed class CommissionTests
{
    [Fact]
    public void Record_Should_Default_To_Pending()
    {
        var commission = Commission.Record(
            Guid.NewGuid(), Money.Create(50m).Value, CommissionSourceType.PersonalTraining, Guid.NewGuid(), DateTimeOffset.UtcNow, null);

        Assert.Equal(CommissionStatus.Pending, commission.Status);
        Assert.Null(commission.PaidOnUtc);
    }

    [Fact]
    public void MarkPaid_Should_Set_Status_And_PaidOnUtc()
    {
        var commission = Commission.Record(
            Guid.NewGuid(), Money.Create(50m).Value, CommissionSourceType.ProductSale, null, DateTimeOffset.UtcNow, null);
        var paidOn = DateTimeOffset.UtcNow;

        var result = commission.MarkPaid(paidOn);

        Assert.True(result.IsSuccess);
        Assert.Equal(CommissionStatus.Paid, commission.Status);
        Assert.Equal(paidOn, commission.PaidOnUtc);
    }

    [Fact]
    public void MarkPaid_Should_Fail_When_Already_Paid()
    {
        var commission = Commission.Record(
            Guid.NewGuid(), Money.Create(50m).Value, CommissionSourceType.ClassSession, null, DateTimeOffset.UtcNow, null);
        commission.MarkPaid(DateTimeOffset.UtcNow);

        var result = commission.MarkPaid(DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Staff.CommissionAlreadyPaid", result.Error.Code);
    }
}
