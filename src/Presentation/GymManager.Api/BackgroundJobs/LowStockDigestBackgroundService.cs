using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Api.BackgroundJobs;

/// <summary>
/// Once a day, records a single consolidated in-app notification listing every active product currently at
/// or below its reorder threshold. This complements (not replaces) the real-time per-product alert raised by
/// <c>ProductStockLowDomainEvent</c> when a sale first pushes a product below threshold — that one is
/// immediate but easy to miss in the moment; this is the daily catch-all digest for whoever checks
/// notifications each morning.
/// </summary>
public sealed class LowStockDigestBackgroundService(IServiceProvider serviceProvider, ILogger<LowStockDigestBackgroundService> logger)
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

        var readDb = scope.ServiceProvider.GetRequiredService<IApplicationReadDb>();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var lowStockProducts = await readDb.Products
            .Where(p => p.IsActive && p.StockQuantity <= p.ReorderThreshold)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Name, p.StockQuantity, p.ReorderThreshold })
            .ToListAsync(cancellationToken);

        if (lowStockProducts.Count == 0)
            return;

        const string subject = "Daily low-stock digest";
        var body = string.Join(
            Environment.NewLine,
            lowStockProducts.Select(p => $"{p.Name}: {p.StockQuantity} remaining (reorder at {p.ReorderThreshold})"));

        var notification = Notification.Create(NotificationChannel.InApp, string.Empty, subject, body, null, null);
        notification.MarkSent();

        notificationRepository.Add(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Recorded low-stock digest for {Count} products", lowStockProducts.Count);
    }
}
