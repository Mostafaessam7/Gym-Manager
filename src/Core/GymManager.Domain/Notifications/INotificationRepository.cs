using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Notifications;

public interface INotificationRepository : IRepository<Notification, Guid>;
