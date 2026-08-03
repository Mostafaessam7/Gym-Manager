using GymManager.Application.Abstractions;
using GymManager.Application.Reports;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Notifications;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Api.BackgroundJobs;

/// <summary>
/// Once a day, runs yesterday's <see cref="DailyClosingReportQuery"/> across all branches and records the
/// result as an in-app notification, so a closing summary is waiting for staff each morning instead of only
/// ever being available on-demand via the Reports endpoints.
/// </summary>
public sealed class DailyClosingReportBackgroundService(IServiceProvider serviceProvider, ILogger<DailyClosingReportBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

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

        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var closingDate = dateTimeProvider.TodayUtc.AddDays(-1);
        var report = await dispatcher.Send(new DailyClosingReportQuery(BranchId: null, closingDate), cancellationToken);

        var subject = $"Daily closing report for {report.Date:yyyy-MM-dd}";
        var body = $"Revenue: {report.TotalRevenue:F2} {report.Currency} (Cash {report.CashTotal:F2}, Card {report.CardTotal:F2}, " +
                   $"Other {report.OtherTotal:F2}) | Expenses: {report.TotalExpenses:F2} {report.Currency} | " +
                   $"Sales: {report.SalesCount} | Attendance: {report.AttendanceCount}";

        var notification = Notification.Create(NotificationChannel.InApp, string.Empty, subject, body, null, null);
        notification.MarkSent();

        notificationRepository.Add(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Recorded daily closing report notification for {ClosingDate}", closingDate);
    }
}
