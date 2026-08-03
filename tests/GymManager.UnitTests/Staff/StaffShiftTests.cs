using GymManager.Domain.Staff;
using Xunit;

namespace GymManager.UnitTests.Staff;

public sealed class StaffShiftTests
{
    private static StaffShift CreateShift(DateTimeOffset? start = null, DateTimeOffset? end = null) =>
        StaffShift.Schedule(
            Guid.NewGuid(), Guid.NewGuid(), start ?? DateTimeOffset.UtcNow.AddDays(1),
            end ?? DateTimeOffset.UtcNow.AddDays(1).AddHours(8), "Opening shift").Value;

    [Fact]
    public void Schedule_Should_Fail_When_End_Is_Before_Start()
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var end = start.AddHours(-1);

        var result = StaffShift.Schedule(Guid.NewGuid(), Guid.NewGuid(), start, end, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Staff.ShiftEndBeforeStart", result.Error.Code);
    }

    [Fact]
    public void Schedule_Should_Default_To_Scheduled_Status()
    {
        var shift = CreateShift();

        Assert.Equal(StaffShiftStatus.Scheduled, shift.Status);
    }

    [Fact]
    public void Reschedule_Should_Update_Times_And_Notes()
    {
        var shift = CreateShift();
        var newStart = DateTimeOffset.UtcNow.AddDays(2);
        var newEnd = newStart.AddHours(6);

        var result = shift.Reschedule(newStart, newEnd, "Updated");

        Assert.True(result.IsSuccess);
        Assert.Equal(newStart, shift.StartUtc);
        Assert.Equal("Updated", shift.Notes);
    }

    [Fact]
    public void Complete_Should_Succeed_For_A_Scheduled_Shift()
    {
        var shift = CreateShift();

        var result = shift.Complete();

        Assert.True(result.IsSuccess);
        Assert.Equal(StaffShiftStatus.Completed, shift.Status);
    }

    [Fact]
    public void Complete_Should_Fail_When_Already_Completed()
    {
        var shift = CreateShift();
        shift.Complete();

        var result = shift.Complete();

        Assert.True(result.IsFailure);
        Assert.Equal("Staff.ShiftAlreadyFinalized", result.Error.Code);
    }

    [Fact]
    public void Cancel_Should_Succeed_For_A_Scheduled_Shift()
    {
        var shift = CreateShift();

        var result = shift.Cancel();

        Assert.True(result.IsSuccess);
        Assert.Equal(StaffShiftStatus.Cancelled, shift.Status);
    }

    [Fact]
    public void MarkNoShow_Should_Succeed_For_A_Scheduled_Shift()
    {
        var shift = CreateShift();

        var result = shift.MarkNoShow();

        Assert.True(result.IsSuccess);
        Assert.Equal(StaffShiftStatus.NoShow, shift.Status);
    }

    [Fact]
    public void Reschedule_Should_Fail_For_A_Cancelled_Shift()
    {
        var shift = CreateShift();
        shift.Cancel();

        var result = shift.Reschedule(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), null);

        Assert.True(result.IsFailure);
        Assert.Equal("Staff.ShiftAlreadyFinalized", result.Error.Code);
    }
}
