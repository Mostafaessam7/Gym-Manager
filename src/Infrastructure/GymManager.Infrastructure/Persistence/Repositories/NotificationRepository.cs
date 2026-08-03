using GymManager.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository(GymManagerDbContext dbContext) : INotificationRepository
{
    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public void Add(Notification aggregate) => dbContext.Notifications.Add(aggregate);

    public void Update(Notification aggregate) => dbContext.Notifications.Update(aggregate);

    public void Remove(Notification aggregate) => dbContext.Notifications.Remove(aggregate);
}
