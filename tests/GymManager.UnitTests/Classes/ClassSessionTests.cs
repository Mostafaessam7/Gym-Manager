using GymManager.Domain.Classes;
using Xunit;

namespace GymManager.UnitTests.Classes;

public sealed class ClassSessionTests
{
    private static ClassSession CreateSession(int capacity = 2) =>
        ClassSession.Schedule(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddHours(1), capacity).Value;

    [Fact]
    public void Schedule_Should_Fail_When_End_Is_Before_Start()
    {
        var result = ClassSession.Schedule(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(2), DateTimeOffset.UtcNow, 10);

        Assert.True(result.IsFailure);
        Assert.Equal("ClassSession.EndBeforeStart", result.Error.Code);
    }

    [Fact]
    public void Book_Should_Succeed_When_Spots_Are_Available()
    {
        var session = CreateSession();

        var result = session.Book(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, session.ActiveBookingsCount);
    }

    [Fact]
    public void Book_Should_Fail_When_Member_Already_Has_An_Active_Booking()
    {
        var session = CreateSession();
        var memberId = Guid.NewGuid();
        session.Book(memberId);

        var result = session.Book(memberId);

        Assert.True(result.IsFailure);
        Assert.Equal("ClassSession.AlreadyBooked", result.Error.Code);
    }

    [Fact]
    public void Book_Should_Fail_When_Session_Is_Full()
    {
        var session = CreateSession(capacity: 1);
        session.Book(Guid.NewGuid());

        var result = session.Book(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("ClassSession.SessionFull", result.Error.Code);
    }

    [Fact]
    public void CancelBooking_Should_Free_Up_A_Spot()
    {
        var session = CreateSession(capacity: 1);
        var memberId = Guid.NewGuid();
        session.Book(memberId);

        var cancelResult = session.CancelBooking(memberId);
        var rebookResult = session.Book(Guid.NewGuid());

        Assert.True(cancelResult.IsSuccess);
        Assert.True(rebookResult.IsSuccess);
    }

    [Fact]
    public void Cancel_Should_Cancel_All_Active_Bookings()
    {
        var session = CreateSession();
        session.Book(Guid.NewGuid());
        session.Book(Guid.NewGuid());

        var result = session.Cancel();

        Assert.True(result.IsSuccess);
        Assert.Equal(ClassSessionStatus.Cancelled, session.Status);
        Assert.Equal(0, session.ActiveBookingsCount);
    }

    [Fact]
    public void Book_Should_Fail_When_Session_Already_Cancelled()
    {
        var session = CreateSession();
        session.Cancel();

        var result = session.Book(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("ClassSession.NotScheduled", result.Error.Code);
    }
}
