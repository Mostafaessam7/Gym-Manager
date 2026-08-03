using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members;
using GymManager.Domain.Memberships.Events;
using GymManager.Domain.Notifications;
using GymManager.SharedKernel.Primitives;

namespace GymManager.Application.Notifications.EventHandlers;

/// <summary>
/// Notifies a member when their membership expires (raised either from an explicit lookup or from
/// <c>MembershipExpiryBackgroundService</c>'s daily sweep). Prefers SMS, since a lapsed member is the
/// audience least likely to be checking email for a "come back and renew" nudge; falls back to email.
/// </summary>
public sealed class MembershipExpiredNotificationHandler(
    IMemberRepository memberRepository, IEmailSender emailSender, ISmsSender smsSender,
    INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
    : IDomainEventHandler<MembershipExpiredDomainEvent>
{
    public async Task HandleAsync(MembershipExpiredDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(domainEvent.MemberId, cancellationToken);
        if (member is null)
            return;

        const string subject = "Your membership has expired";
        var body = $"Hi {member.FirstName}, your membership expired. Visit the front desk or renew online to keep your access active.";

        Notification notification;

        if (!string.IsNullOrWhiteSpace(member.PhoneNumber))
        {
            notification = Notification.Create(NotificationChannel.Sms, member.PhoneNumber, subject, body, null, member.Id);
            try
            {
                await smsSender.SendAsync(member.PhoneNumber, body, cancellationToken);
                notification.MarkSent();
            }
            catch (Exception exception)
            {
                notification.MarkFailed(exception.Message);
            }
        }
        else if (member.Email is not null)
        {
            notification = Notification.Create(NotificationChannel.Email, member.Email.Value, subject, body, null, member.Id);
            try
            {
                await emailSender.SendAsync(member.Email.Value, subject, body, cancellationToken);
                notification.MarkSent();
            }
            catch (Exception exception)
            {
                notification.MarkFailed(exception.Message);
            }
        }
        else
        {
            return;
        }

        notificationRepository.Add(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
