using GymManager.Domain.Attendance;
using Xunit;

namespace GymManager.UnitTests.Attendance;

public sealed class AttendanceRecordTests
{
    [Fact]
    public void CheckIn_Should_Raise_MemberCheckedInDomainEvent_And_Leave_Session_Open()
    {
        var record = AttendanceRecord.CheckIn(Guid.NewGuid(), Guid.NewGuid(), CheckInMethod.QrCode);

        Assert.Single(record.DomainEvents);
        Assert.True(record.IsOpen);
        Assert.Null(record.CheckOutUtc);
    }

    [Fact]
    public void CheckOut_Should_Close_The_Session()
    {
        var record = AttendanceRecord.CheckIn(Guid.NewGuid(), Guid.NewGuid(), CheckInMethod.Manual, Guid.NewGuid());

        var result = record.CheckOut();

        Assert.True(result.IsSuccess);
        Assert.False(record.IsOpen);
        Assert.NotNull(record.CheckOutUtc);
    }

    [Fact]
    public void CheckOut_Should_Fail_When_Already_Checked_Out()
    {
        var record = AttendanceRecord.CheckIn(Guid.NewGuid(), Guid.NewGuid(), CheckInMethod.Barcode);
        record.CheckOut();

        var result = record.CheckOut();

        Assert.True(result.IsFailure);
        Assert.Equal("Attendance.AlreadyCheckedOut", result.Error.Code);
    }
}
