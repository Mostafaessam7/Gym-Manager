using GymManager.Application.AuditLogs.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.AuditLogs.GetAuditLogs;

public sealed record GetAuditLogsQuery(PaginationParameters Pagination, string? EntityName, string? EntityId, Guid? UserId)
    : IQuery<PagedList<AuditLogResponse>>;
