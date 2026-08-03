using GymManager.Domain.Abstractions;
using GymManager.Domain.Notifications;
using GymManager.Domain.Products.Events;
using GymManager.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;

namespace GymManager.Application.Notifications.EventHandlers;

/// <summary>
/// Records an in-app low-stock alert when a product's stock drops to or below its reorder threshold. Unlike
/// the member-facing handlers, this has no single recipient — it targets whichever staff have
/// <c>inventory:view</c> and check the notifications feed — so it's stored with no recipient rather than
/// emailed or texted to anyone in particular.
/// </summary>
public sealed class LowStockAlertOnProductStockLowHandler(
    INotificationRepository notificationRepository, IUnitOfWork unitOfWork, ILogger<LowStockAlertOnProductStockLowHandler> logger)
    : IDomainEventHandler<ProductStockLowDomainEvent>
{
    public async Task HandleAsync(ProductStockLowDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "Product {ProductName} ({ProductId}) is low on stock: {Remaining} remaining, reorder threshold is {Threshold}",
            domainEvent.Name, domainEvent.ProductId, domainEvent.RemainingQuantity, domainEvent.ReorderThreshold);

        const string subject = "Low stock alert";
        var body = $"{domainEvent.Name} is low on stock: {domainEvent.RemainingQuantity} remaining (reorder threshold {domainEvent.ReorderThreshold}).";

        var notification = Notification.Create(NotificationChannel.InApp, string.Empty, subject, body, null, null);
        notification.MarkSent();

        notificationRepository.Add(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
