using GymManager.Domain.Staff.Errors;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Staff;

/// <summary>A staff member's request for time off, awaiting a manager's decision.</summary>
public sealed class LeaveRequest : AggregateRoot<Guid>, IAuditableEntity
{
    private LeaveRequest()
    {
    }

    private LeaveRequest(Guid id, Guid userId, LeaveType type, DateOnly startDate, DateOnly endDate, string? reason)
        : base(id)
    {
        UserId = userId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Reason = reason;
        Status = LeaveRequestStatus.Pending;
    }

    public Guid UserId { get; private set; }

    public LeaveType Type { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public string? Reason { get; private set; }

    public LeaveRequestStatus Status { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTimeOffset? DecidedOnUtc { get; private set; }

    public string? DecisionNotes { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static Result<LeaveRequest> Request(Guid userId, LeaveType type, DateOnly startDate, DateOnly endDate, string? reason)
    {
        if (endDate < startDate)
            return Result.Failure<LeaveRequest>(StaffErrors.LeaveEndBeforeStart);

        return Result.Success(new LeaveRequest(Guid.NewGuid(), userId, type, startDate, endDate, reason?.Trim()));
    }

    public Result Approve(Guid decidedByUserId, string? notes, DateTimeOffset utcNow)
    {
        if (Status != LeaveRequestStatus.Pending)
            return Result.Failure(StaffErrors.LeaveRequestAlreadyDecided);

        Status = LeaveRequestStatus.Approved;
        DecidedByUserId = decidedByUserId;
        DecidedOnUtc = utcNow;
        DecisionNotes = notes?.Trim();
        return Result.Success();
    }

    public Result Reject(Guid decidedByUserId, string? notes, DateTimeOffset utcNow)
    {
        if (Status != LeaveRequestStatus.Pending)
            return Result.Failure(StaffErrors.LeaveRequestAlreadyDecided);

        Status = LeaveRequestStatus.Rejected;
        DecidedByUserId = decidedByUserId;
        DecidedOnUtc = utcNow;
        DecisionNotes = notes?.Trim();
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
