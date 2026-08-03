using GymManager.Domain.Staff.Errors;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Staff;

/// <summary>A scheduled work shift for a staff member (any <c>User</c>, not just trainers — front desk,
/// managers, etc. are scheduled the same way).</summary>
public sealed class StaffShift : AggregateRoot<Guid>, IAuditableEntity
{
    private StaffShift()
    {
    }

    private StaffShift(Guid id, Guid userId, Guid branchId, DateTimeOffset startUtc, DateTimeOffset endUtc, string? notes)
        : base(id)
    {
        UserId = userId;
        BranchId = branchId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Notes = notes;
        Status = StaffShiftStatus.Scheduled;
    }

    public Guid UserId { get; private set; }

    public Guid BranchId { get; private set; }

    public DateTimeOffset StartUtc { get; private set; }

    public DateTimeOffset EndUtc { get; private set; }

    public StaffShiftStatus Status { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static Result<StaffShift> Schedule(Guid userId, Guid branchId, DateTimeOffset startUtc, DateTimeOffset endUtc, string? notes)
    {
        if (endUtc <= startUtc)
            return Result.Failure<StaffShift>(StaffErrors.ShiftEndBeforeStart);

        return Result.Success(new StaffShift(Guid.NewGuid(), userId, branchId, startUtc, endUtc, notes?.Trim()));
    }

    public Result Reschedule(DateTimeOffset startUtc, DateTimeOffset endUtc, string? notes)
    {
        if (Status != StaffShiftStatus.Scheduled)
            return Result.Failure(StaffErrors.ShiftAlreadyFinalized);

        if (endUtc <= startUtc)
            return Result.Failure(StaffErrors.ShiftEndBeforeStart);

        StartUtc = startUtc;
        EndUtc = endUtc;
        Notes = notes?.Trim();
        return Result.Success();
    }

    public Result Complete()
    {
        if (Status != StaffShiftStatus.Scheduled)
            return Result.Failure(StaffErrors.ShiftAlreadyFinalized);

        Status = StaffShiftStatus.Completed;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status != StaffShiftStatus.Scheduled)
            return Result.Failure(StaffErrors.ShiftAlreadyFinalized);

        Status = StaffShiftStatus.Cancelled;
        return Result.Success();
    }

    public Result MarkNoShow()
    {
        if (Status != StaffShiftStatus.Scheduled)
            return Result.Failure(StaffErrors.ShiftAlreadyFinalized);

        Status = StaffShiftStatus.NoShow;
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
