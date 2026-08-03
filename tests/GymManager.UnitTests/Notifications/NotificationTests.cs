using GymManager.Domain.Notifications;
using Xunit;

namespace GymManager.UnitTests.Notifications;

public sealed class NotificationTests
{
    [Fact]
    public void MarkSent_Should_Succeed_For_Pending_Notification()
    {
        var notification = Notification.Create(NotificationChannel.Email, "member@gym.io", "Welcome", "Hi there", null, Guid.NewGuid());

        var result = notification.MarkSent();

        Assert.True(result.IsSuccess);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.NotNull(notification.SentOnUtc);
    }

    [Fact]
    public void MarkFailed_Should_Record_ErrorMessage()
    {
        var notification = Notification.Create(NotificationChannel.Sms, "+15551234567", "Reminder", "Class soon", null, Guid.NewGuid());

        var result = notification.MarkFailed("SMTP timeout");

        Assert.True(result.IsSuccess);
        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal("SMTP timeout", notification.ErrorMessage);
    }

    [Fact]
    public void MarkSent_Should_Fail_When_Already_Resolved()
    {
        var notification = Notification.Create(NotificationChannel.Email, "member@gym.io", "Welcome", "Hi there", null, Guid.NewGuid());
        notification.MarkSent();

        var result = notification.MarkSent();

        Assert.True(result.IsFailure);
        Assert.Equal("Notification.NotPending", result.Error.Code);
    }
}
