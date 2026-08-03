using GymManager.Application.Notifications.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Notifications.GetNotifications;

public sealed record GetNotificationsQuery(PaginationParameters Pagination, Guid? RecipientUserId, Guid? RecipientMemberId, string? Status)
    : IQuery<PagedList<NotificationResponse>>;
