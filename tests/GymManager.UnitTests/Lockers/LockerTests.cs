using GymManager.Domain.Lockers;
using Xunit;

namespace GymManager.UnitTests.Lockers;

public sealed class LockerTests
{
    [Fact]
    public void AssignTo_Should_Succeed_When_Available()
    {
        var locker = Locker.Create(Guid.NewGuid(), "A-01");
        var memberId = Guid.NewGuid();

        var result = locker.AssignTo(memberId);

        Assert.True(result.IsSuccess);
        Assert.Equal(LockerStatus.Assigned, locker.Status);
        Assert.Equal(memberId, locker.AssignedMemberId);
    }

    [Fact]
    public void AssignTo_Should_Fail_When_Already_Assigned()
    {
        var locker = Locker.Create(Guid.NewGuid(), "A-01");
        locker.AssignTo(Guid.NewGuid());

        var result = locker.AssignTo(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Locker.NotAvailable", result.Error.Code);
    }

    [Fact]
    public void Release_Should_Fail_When_Not_Assigned()
    {
        var locker = Locker.Create(Guid.NewGuid(), "A-01");

        var result = locker.Release();

        Assert.True(result.IsFailure);
        Assert.Equal("Locker.NotAssigned", result.Error.Code);
    }

    [Fact]
    public void Release_Should_Clear_Assignment()
    {
        var locker = Locker.Create(Guid.NewGuid(), "A-01");
        locker.AssignTo(Guid.NewGuid());

        var result = locker.Release();

        Assert.True(result.IsSuccess);
        Assert.Equal(LockerStatus.Available, locker.Status);
        Assert.Null(locker.AssignedMemberId);
    }

    [Fact]
    public void SetUnderMaintenance_Should_Fail_When_Currently_Assigned()
    {
        var locker = Locker.Create(Guid.NewGuid(), "A-01");
        locker.AssignTo(Guid.NewGuid());

        var result = locker.SetUnderMaintenance();

        Assert.True(result.IsFailure);
        Assert.Equal("Locker.NotAvailable", result.Error.Code);
    }
}
