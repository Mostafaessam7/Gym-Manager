using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Crm;

/// <summary>A scheduled (and eventually completed) touchpoint with a lead — a call, email, or meeting.</summary>
public sealed class LeadFollowUp : Entity<Guid>
{
    private LeadFollowUp()
    {
    }

    internal LeadFollowUp(FollowUpType type, DateTimeOffset scheduledOnUtc, string? notes)
        : base(Guid.NewGuid())
    {
        Type = type;
        ScheduledOnUtc = scheduledOnUtc;
        Notes = notes;
    }

    public FollowUpType Type { get; private set; }

    public DateTimeOffset ScheduledOnUtc { get; private set; }

    public DateTimeOffset? CompletedOnUtc { get; private set; }

    public string? Notes { get; private set; }

    public bool IsCompleted => CompletedOnUtc is not null;

    internal void Complete(DateTimeOffset completedOnUtc, string? notes)
    {
        CompletedOnUtc = completedOnUtc;
        if (notes is not null)
            Notes = notes;
    }
}
