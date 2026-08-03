using GymManager.Application.Abstractions;
using GymManager.Application.AuditLogs.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.AuditLogs.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler(IApplicationReadDb readDb) : IQueryHandler<GetAuditLogsQuery, PagedList<AuditLogResponse>>
{
    public async Task<PagedList<AuditLogResponse>> Handle(GetAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var logs = readDb.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.EntityName))
            logs = logs.Where(l => l.EntityName == query.EntityName);

        if (!string.IsNullOrWhiteSpace(query.EntityId))
            logs = logs.Where(l => l.EntityId == query.EntityId);

        if (query.UserId.HasValue)
            logs = logs.Where(l => l.UserId == query.UserId);

        var totalCount = await logs.CountAsync(cancellationToken);

        var items = await logs
            .OrderByDescending(l => l.TimestampUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(l => new AuditLogResponse(l.Id, l.EntityName, l.EntityId, l.Action.ToString(), l.Changes, l.UserId, l.UserEmail, l.TimestampUtc))
            .ToListAsync(cancellationToken);

        return new PagedList<AuditLogResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
