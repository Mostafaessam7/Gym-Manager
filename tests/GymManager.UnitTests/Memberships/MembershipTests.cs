using GymManager.Domain.Common;
using GymManager.Domain.Memberships;
using Xunit;

namespace GymManager.UnitTests.Memberships;

public sealed class MembershipTests
{
    private static Membership CreatePurchasedMembership(DateOnly startDate, int durationDays = 30) =>
        Membership.Purchase(Guid.NewGuid(), Guid.NewGuid(), "Monthly", startDate, durationDays, Money.Create(50m).Value);

    [Fact]
    public void Purchase_Should_Set_EndDate_Based_On_Duration()
    {
        var startDate = new DateOnly(2026, 1, 1);
        var membership = CreatePurchasedMembership(startDate, 30);

        Assert.Equal(startDate.AddDays(30), membership.EndDate);
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.Single(membership.DomainEvents);
    }

    [Fact]
    public void Renew_From_Active_Membership_Should_Extend_From_Current_EndDate()
    {
        var startDate = new DateOnly(2026, 1, 1);
        var membership = CreatePurchasedMembership(startDate, 30);
        var today = new DateOnly(2026, 1, 10);

        var result = membership.Renew(30, Money.Create(50m).Value, today);

        Assert.True(result.IsSuccess);
        Assert.Equal(startDate.AddDays(60), membership.EndDate);
        Assert.Single(membership.Renewals);
    }

    [Fact]
    public void Renew_After_Expiry_Should_Extend_From_Today_Not_The_Stale_EndDate()
    {
        var startDate = new DateOnly(2026, 1, 1);
        var membership = CreatePurchasedMembership(startDate, 30);
        var today = new DateOnly(2026, 3, 1);

        membership.Renew(30, Money.Create(50m).Value, today);

        Assert.Equal(today.AddDays(30), membership.EndDate);
    }

    [Fact]
    public void Renew_Should_Fail_For_Cancelled_Membership()
    {
        var membership = CreatePurchasedMembership(new DateOnly(2026, 1, 1));
        membership.Cancel();

        var result = membership.Renew(30, Money.Create(50m).Value, new DateOnly(2026, 1, 10));

        Assert.True(result.IsFailure);
        Assert.Equal("Membership.CannotRenewCancelled", result.Error.Code);
    }

    [Fact]
    public void Freeze_Should_Fail_When_Not_Active()
    {
        var membership = CreatePurchasedMembership(new DateOnly(2026, 1, 1));
        membership.Cancel();

        var result = membership.Freeze();

        Assert.True(result.IsFailure);
        Assert.Equal("Membership.NotActive", result.Error.Code);
    }

    [Fact]
    public void Cancel_Should_Fail_When_Already_Cancelled()
    {
        var membership = CreatePurchasedMembership(new DateOnly(2026, 1, 1));
        membership.Cancel();

        var result = membership.Cancel();

        Assert.True(result.IsFailure);
        Assert.Equal("Membership.AlreadyCancelled", result.Error.Code);
    }

    [Fact]
    public void MarkExpired_Should_Transition_Active_Past_EndDate_Membership_To_Expired()
    {
        var membership = CreatePurchasedMembership(new DateOnly(2026, 1, 1), 10);

        membership.MarkExpired(new DateOnly(2026, 1, 15));

        Assert.Equal(MembershipStatus.Expired, membership.Status);
    }

    [Fact]
    public void MarkExpired_Should_Not_Affect_Membership_Still_Within_Term()
    {
        var membership = CreatePurchasedMembership(new DateOnly(2026, 1, 1), 30);

        membership.MarkExpired(new DateOnly(2026, 1, 15));

        Assert.Equal(MembershipStatus.Active, membership.Status);
    }
}
