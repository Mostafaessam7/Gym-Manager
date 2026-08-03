namespace GymManager.Domain.Notifications;

public enum NotificationChannel
{
    Email = 0,
    Sms = 1,
    InApp = 2,
}

public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
}
