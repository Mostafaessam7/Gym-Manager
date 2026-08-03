using GymManager.Domain.Staff;
using Xunit;

namespace GymManager.UnitTests.Staff;

public sealed class LeaveRequestTests
{
    private static LeaveRequest CreateLeaveRequest() =>
        LeaveRequest.Request(
            Guid.NewGuid(), LeaveType.Vacation, new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 15), "Family trip").Value;

    [Fact]
    public void Request_Should_Fail_When_EndDate_Is_Before_StartDate()
    {
        var result = LeaveRequest.Request(Guid.NewGuid(), LeaveType.Sick, new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 5), null);

        Assert.True(result.IsFailure);
        Assert.Equal("Staff.LeaveEndBeforeStart", result.Error.Code);
    }

    [Fact]
    public void Request_Should_Default_To_Pending()
    {
        var leaveRequest = CreateLeaveRequest();

        Assert.Equal(LeaveRequestStatus.Pending, leaveRequest.Status);
    }

    [Fact]
    public void Approve_Should_Set_Status_And_Decision_Metadata()
    {
        var leaveRequest = CreateLeaveRequest();
        var managerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var result = leaveRequest.Approve(managerId, "Enjoy!", now);

        Assert.True(result.IsSuccess);
        Assert.Equal(LeaveRequestStatus.Approved, leaveRequest.Status);
        Assert.Equal(managerId, leaveRequest.DecidedByUserId);
        Assert.Equal(now, leaveRequest.DecidedOnUtc);
        Assert.Equal("Enjoy!", leaveRequest.DecisionNotes);
    }

    [Fact]
    public void Reject_Should_Set_Status_And_Decision_Metadata()
    {
        var leaveRequest = CreateLeaveRequest();
        var managerId = Guid.NewGuid();

        var result = leaveRequest.Reject(managerId, "Short-staffed", DateTimeOffset.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(LeaveRequestStatus.Rejected, leaveRequest.Status);
    }

    [Fact]
    public void Approve_Should_Fail_When_Already_Decided()
    {
        var leaveRequest = CreateLeaveRequest();
        leaveRequest.Approve(Guid.NewGuid(), null, DateTimeOffset.UtcNow);

        var result = leaveRequest.Reject(Guid.NewGuid(), null, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Staff.LeaveRequestAlreadyDecided", result.Error.Code);
    }
}
