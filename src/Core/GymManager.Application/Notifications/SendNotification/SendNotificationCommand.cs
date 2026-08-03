using GymManager.Application.Notifications.Contracts;
using GymManager.Domain.Notifications;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Notifications.SendNotification;

public sealed record SendNotificationCommand(
    NotificationChannel Channel, string RecipientAddress, string Subject, string Body, Guid? RecipientUserId, Guid? RecipientMemberId)
    : ICommand<Result<NotificationResponse>>;
