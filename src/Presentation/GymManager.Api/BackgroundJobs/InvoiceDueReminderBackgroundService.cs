using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Invoices;
using GymManager.Domain.Members;
using GymManager.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Api.BackgroundJobs;

/// <summary>
/// Once a day, reminds every member with an issued (unpaid) invoice due in exactly 3 days. As with the
/// membership-expiring reminder, firing on the exact due-in-3-days date — rather than "due within 3 days" —
/// keeps this a one-time reminder per invoice without needing to track "already reminded" on the aggregate.
/// </summary>
public sealed class InvoiceDueReminderBackgroundService(IServiceProvider serviceProvider, ILogger<InvoiceDueReminderBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private const int ReminderLeadDays = 3;

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

        var readDb = scope.ServiceProvider.GetRequiredService<IApplicationReadDb>();
        var memberRepository = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var smsSender = scope.ServiceProvider.GetRequiredService<ISmsSender>();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var reminderDate = dateTimeProvider.TodayUtc.AddDays(ReminderLeadDays);

        // TotalAmount/Currency are computed from the owned Lines collection and can't be translated to SQL,
        // so invoices are materialized first and the date/amount filtering happens in-memory afterward.
        var dueOnReminderDate = (await readDb.Invoices
                .Where(i => i.Status == InvoiceStatus.Issued)
                .ToListAsync(cancellationToken))
            .Where(i => DateOnly.FromDateTime(i.DueOnUtc.UtcDateTime) == reminderDate)
            .Select(i => new { i.InvoiceNumber, i.MemberId, i.TotalAmount.Amount, i.Currency })
            .ToList();

        var sentCount = 0;

        foreach (var invoice in dueOnReminderDate)
        {
            var member = await memberRepository.GetByIdAsync(invoice.MemberId, cancellationToken);
            if (member is null)
                continue;

            const string subject = "Invoice due soon";
            var body = $"Hi {member.FirstName}, invoice {invoice.InvoiceNumber} for {invoice.Amount:F2} {invoice.Currency} " +
                       $"is due on {reminderDate:yyyy-MM-dd}.";

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
            logger.LogInformation("Sent {Count} invoice-due reminders for {ReminderDate}", sentCount, reminderDate);
        }
    }
}
