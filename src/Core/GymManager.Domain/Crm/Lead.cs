using GymManager.Domain.Crm.Errors;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Crm;

/// <summary>A prospective member moving through the sales pipeline — from first contact through to either
/// conversion (an actual <c>Member</c> record) or being marked lost.</summary>
public sealed class Lead : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<LeadFollowUp> _followUps = [];

    private Lead()
    {
        Name = string.Empty;
    }

    private Lead(Guid id, string name, string? email, string? phone, LeadSource source, Guid? branchId, Guid? assignedToUserId, string? notes)
        : base(id)
    {
        Name = name;
        Email = email;
        Phone = phone;
        Source = source;
        BranchId = branchId;
        AssignedToUserId = assignedToUserId;
        Notes = notes;
        Stage = LeadStage.New;
    }

    public string Name { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public LeadSource Source { get; private set; }

    public LeadStage Stage { get; private set; }

    public Guid? BranchId { get; private set; }

    public Guid? AssignedToUserId { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>Set once this lead is converted via <see cref="ConvertToMember"/>; null until then.</summary>
    public Guid? ConvertedMemberId { get; private set; }

    public string? LostReason { get; private set; }

    public IReadOnlyCollection<LeadFollowUp> FollowUps => _followUps.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static Lead Create(string name, string? email, string? phone, LeadSource source, Guid? branchId, Guid? assignedToUserId, string? notes) =>
        new(Guid.NewGuid(), name.Trim(), email?.Trim(), phone?.Trim(), source, branchId, assignedToUserId, notes?.Trim());

    public void UpdateDetails(string name, string? email, string? phone, LeadSource source, string? notes)
    {
        Name = name.Trim();
        Email = email?.Trim();
        Phone = phone?.Trim();
        Source = source;
        Notes = notes?.Trim();
    }

    public void AssignTo(Guid? userId) => AssignedToUserId = userId;

    /// <summary>Moves the lead to any non-terminal stage. Use <see cref="MarkLost"/> or
    /// <see cref="ConvertToMember"/> to reach a terminal state, since those carry their own bookkeeping.</summary>
    public Result MoveToStage(LeadStage stage)
    {
        if (stage is LeadStage.Won or LeadStage.Lost)
            return Result.Failure(LeadErrors.NotWon);

        if (ConvertedMemberId is not null)
            return Result.Failure(LeadErrors.AlreadyConverted);

        Stage = stage;
        return Result.Success();
    }

    public Result MarkLost(string? reason)
    {
        if (ConvertedMemberId is not null)
            return Result.Failure(LeadErrors.AlreadyConverted);

        Stage = LeadStage.Lost;
        LostReason = reason?.Trim();
        return Result.Success();
    }

    /// <summary>Reopens a lead previously marked <see cref="LeadStage.Lost"/> back into the active pipeline.</summary>
    public Result Reopen()
    {
        if (ConvertedMemberId is not null)
            return Result.Failure(LeadErrors.AlreadyConverted);

        Stage = LeadStage.Contacted;
        LostReason = null;
        return Result.Success();
    }

    /// <summary>Marks the lead won and links it to the <c>Member</c> record created from it — the caller
    /// (application layer) is responsible for actually creating that member, since <c>Member</c> lives in a
    /// different aggregate the domain layer here doesn't reference.</summary>
    public Result ConvertToMember(Guid memberId)
    {
        if (ConvertedMemberId is not null)
            return Result.Failure(LeadErrors.AlreadyConverted);

        Stage = LeadStage.Won;
        ConvertedMemberId = memberId;
        return Result.Success();
    }

    public LeadFollowUp AddFollowUp(FollowUpType type, DateTimeOffset scheduledOnUtc, string? notes)
    {
        var followUp = new LeadFollowUp(type, scheduledOnUtc, notes?.Trim());
        _followUps.Add(followUp);
        return followUp;
    }

    public Result CompleteFollowUp(Guid followUpId, DateTimeOffset completedOnUtc, string? notes)
    {
        var followUp = _followUps.FirstOrDefault(f => f.Id == followUpId);
        if (followUp is null)
            return Result.Failure(LeadErrors.FollowUpNotFound);

        followUp.Complete(completedOnUtc, notes?.Trim());
        return Result.Success();
    }

    public void SetCreated(DateTimeOffset onUtc, string? by)
    {
        CreatedOnUtc = onUtc;
        CreatedBy = by;
    }

    public void SetModified(DateTimeOffset onUtc, string? by)
    {
        ModifiedOnUtc = onUtc;
        ModifiedBy = by;
    }
}
