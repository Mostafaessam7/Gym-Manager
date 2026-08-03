using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members;
using GymManager.Domain.Memberships;
using GymManager.Domain.Notifications;

namespace GymManager.Api.BackgroundJobs;

/// <summary>
/// Once a day, reminds every member whose active membership expires in exactly 7 days. Firing on the exact
/// day (rather than "expires within 7 days", which would re-notify daily as the window keeps including the
/// same membership) is what keeps this a one-time reminder per membership without needing extra state on the
/// aggregate to track "already reminded".
/// </summary>
public sealed class MembershipExpiringSoonReminderBackgroundService(
    IServiceProvider serviceProvider, ILogger<MembershipExpiringSoonReminderBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private const int ReminderLeadDays = 7;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var membershipRepository = scope.ServiceProvider.GetRequiredService<IMembershipRepository>();
        var memberRepository = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var smsSender = scope.ServiceProvider.GetRequiredService<ISmsSender>();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var reminderDate = dateTimeProvider.TodayUtc.AddDays(ReminderLeadDays);
        var expiringOnReminderDate = await membershipRepository.GetActiveMembershipsExpiringBetweenAsync(
            reminderDate, reminderDate, cancellationToken);

        var sentCount = 0;

        foreach (var membership in expiringOnReminderDate)
        {
            var member = await memberRepository.GetByIdAsync(membership.MemberId, cancellationToken);
            if (member is null)
                continue;

            const string subject = "Your membership expires in 7 days";
            var body = $"Hi {member.FirstName}, your membership expires on {membership.EndDate:yyyy-MM-dd}. Renew now to keep your access active.";

            Notification notification;
            try
            {
                if (!string.IsNullOrWhiteSpace(member.PhoneNumber))
                {
                    notification = Notification.Create(NotificationChannel.Sms, member.PhoneNumber, subject, body, null, member.Id);
                    await smsSender.SendAsync(member.PhoneNumber, body, cancellationToken);
                }
                else if (member.Email is not null)
                {
                    notification = Notification.Create(NotificationChannel.Email, member.Email.Value, subject, body, null, member.Id);
                    await emailSender.SendAsync(member.Email.Value, subject, body, cancellationToken);
                }
                else
                {
                    continue;
                }

                notification.MarkSent();
            }
            catch (Exception exception)
            {
                notification = Notification.Create(NotificationChannel.Sms, member.PhoneNumber, subject, body, null, member.Id);
                notification.MarkFailed(exception.Message);
            }

            notificationRepository.Add(notification);
            sentCount++;
        }

        if (sentCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Sent {Count} membership-expiring-soon reminders for {ReminderDate}", sentCount, reminderDate);
        }
    }
}
