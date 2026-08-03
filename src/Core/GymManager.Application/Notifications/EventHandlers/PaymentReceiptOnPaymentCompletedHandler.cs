using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members;
using GymManager.Domain.Notifications;
using GymManager.Domain.Payments.Events;
using GymManager.SharedKernel.Primitives;

namespace GymManager.Application.Notifications.EventHandlers;

/// <summary>Emails a payment receipt to the member when a payment completes, if they provided an email address.</summary>
public sealed class PaymentReceiptOnPaymentCompletedHandler(
    IMemberRepository memberRepository, IEmailSender emailSender, INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
    : IDomainEventHandler<PaymentCompletedDomainEvent>
{
    public async Task HandleAsync(PaymentCompletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(domainEvent.MemberId, cancellationToken);
        if (member?.Email is null)
            return;

        const string subject = "Payment receipt";
        var body = $"Hi {member.FirstName}, we've received your payment of {domainEvent.Amount:F2} {domainEvent.Currency}. Thank you!";

        var notification = Notification.Create(NotificationChannel.Email, member.Email.Value, subject, body, null, member.Id);

        try
        {
            await emailSender.SendAsync(member.Email.Value, subject, body, cancellationToken);
            notification.MarkSent();
        }
        catch (Exception exception)
        {
            notification.MarkFailed(exception.Message);
        }

        notificationRepository.Add(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
