using GymManager.Application.Abstractions;
using GymManager.Application.Notifications.Contracts;
using GymManager.Domain.Notifications;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Notifications.GetNotifications;

public sealed class GetNotificationsQueryHandler(IApplicationReadDb readDb) : IQueryHandler<GetNotificationsQuery, PagedList<NotificationResponse>>
{
    public async Task<PagedList<NotificationResponse>> Handle(GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var notifications = readDb.Notifications.AsQueryable();

        if (query.RecipientUserId.HasValue)
            notifications = notifications.Where(n => n.RecipientUserId == query.RecipientUserId);

        if (query.RecipientMemberId.HasValue)
            notifications = notifications.Where(n => n.RecipientMemberId == query.RecipientMemberId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<NotificationStatus>(query.Status, true, out var status))
            notifications = notifications.Where(n => n.Status == status);

        var totalCount = await notifications.CountAsync(cancellationToken);

        var items = await notifications
            .OrderByDescending(n => n.CreatedOnUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<NotificationResponse>(items.Select(n => n.ToResponse()).ToList(), pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
