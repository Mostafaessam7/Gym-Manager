using GymManager.Domain.Notifications;

namespace GymManager.Application.Notifications.Contracts;

public sealed record NotificationResponse(
    Guid Id,
    string Channel,
    string RecipientAddress,
    string Subject,
    string Status,
    string? ErrorMessage,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? SentOnUtc);

public static class NotificationMappingExtensions
{
    public static NotificationResponse ToResponse(this Notification notification) => new(
        notification.Id, notification.Channel.ToString(), notification.RecipientAddress, notification.Subject,
        notification.Status.ToString(), notification.ErrorMessage, notification.CreatedOnUtc, notification.SentOnUtc);
}
