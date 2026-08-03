using GymManager.Application.Abstractions;
using GymManager.Application.Notifications.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Notifications;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Notifications.SendNotification;

public sealed class SendNotificationCommandHandler(
    INotificationRepository notificationRepository, IEmailSender emailSender, ISmsSender smsSender, IUnitOfWork unitOfWork)
    : ICommandHandler<SendNotificationCommand, Result<NotificationResponse>>
{
    public async Task<Result<NotificationResponse>> Handle(SendNotificationCommand command, CancellationToken cancellationToken)
    {
        var notification = Notification.Create(
            command.Channel, command.RecipientAddress, command.Subject, command.Body, command.RecipientUserId, command.RecipientMemberId);

        try
        {
            switch (command.Channel)
            {
                case NotificationChannel.Email:
                    await emailSender.SendAsync(command.RecipientAddress, command.Subject, command.Body, cancellationToken);
                    break;
                case NotificationChannel.Sms:
                    await smsSender.SendAsync(command.RecipientAddress, command.Body, cancellationToken);
                    break;
                case NotificationChannel.InApp:
                    break;
            }

            notification.MarkSent();
        }
        catch (Exception exception)
        {
            notification.MarkFailed(exception.Message);
        }

        notificationRepository.Add(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(notification.ToResponse());
    }
}
