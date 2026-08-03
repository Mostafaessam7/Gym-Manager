namespace GymManager.Application.AuditLogs.Contracts;

public sealed record AuditLogResponse(
    Guid Id, string EntityName, string EntityId, string Action, string Changes, Guid? UserId, string? UserEmail, DateTimeOffset TimestampUtc);
