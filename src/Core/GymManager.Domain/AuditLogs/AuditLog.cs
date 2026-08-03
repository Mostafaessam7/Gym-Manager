using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.AuditLogs;

/// <summary>
/// An immutable record of a single change made to an audited entity. Written once at commit time by the
/// persistence layer and never modified afterward — there is no domain behavior to protect here, only a
/// factual log entry.
/// </summary>
public sealed class AuditLog : Entity<Guid>
{
    private AuditLog()
    {
        EntityName = string.Empty;
        EntityId = string.Empty;
        Changes = string.Empty;
    }

    public AuditLog(string entityName, string entityId, AuditAction action, string changes, Guid? userId, string? userEmail)
        : base(Guid.NewGuid())
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        Changes = changes;
        UserId = userId;
        UserEmail = userEmail;
        TimestampUtc = DateTimeOffset.UtcNow;
    }

    public string EntityName { get; private set; }

    public string EntityId { get; private set; }

    public AuditAction Action { get; private set; }

    /// <summary>A JSON object of the properties that changed, each mapped to its old and new value.</summary>
    public string Changes { get; private set; }

    public Guid? UserId { get; private set; }

    public string? UserEmail { get; private set; }

    public DateTimeOffset TimestampUtc { get; private set; }
}
