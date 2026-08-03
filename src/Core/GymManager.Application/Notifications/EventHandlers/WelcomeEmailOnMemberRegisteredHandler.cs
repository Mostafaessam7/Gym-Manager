using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members;
using GymManager.Domain.Members.Events;
using GymManager.Domain.Notifications;
using GymManager.SharedKernel.Primitives;

namespace GymManager.Application.Notifications.EventHandlers;

/// <summary>Sends a welcome email when a new member is registered, if they provided an email address.</summary>
public sealed class WelcomeEmailOnMemberRegisteredHandler(
    IMemberRepository memberRepository, IEmailSender emailSender, INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
    : IDomainEventHandler<MemberRegisteredDomainEvent>
{
    public async Task HandleAsync(MemberRegisteredDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(domainEvent.MemberId, cancellationToken);
        if (member?.Email is null)
            return;

        const string subject = "Welcome to Gym Manager!";
        var body = $"Hi {member.FirstName}, welcome aboard! Your member code is {member.MemberCode}. " +
                   "Show your check-in QR code at the front desk on your first visit.";

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
