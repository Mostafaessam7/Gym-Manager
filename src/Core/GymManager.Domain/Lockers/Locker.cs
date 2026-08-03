using GymManager.Domain.Lockers.Errors;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Lockers;

/// <summary>A physical storage locker at a branch, assignable to one member at a time.</summary>
public sealed class Locker : AggregateRoot<Guid>, IAuditableEntity
{
    private Locker()
    {
        Number = string.Empty;
    }

    private Locker(Guid id, Guid branchId, string number) : base(id)
    {
        BranchId = branchId;
        Number = number;
        Status = LockerStatus.Available;
    }

    public Guid BranchId { get; private set; }

    public string Number { get; private set; }

    public LockerStatus Status { get; private set; }

    public Guid? AssignedMemberId { get; private set; }

    public DateTimeOffset? AssignedOnUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static Locker Create(Guid branchId, string number) => new(Guid.NewGuid(), branchId, number.Trim());

    public Result AssignTo(Guid memberId)
    {
        if (Status != LockerStatus.Available)
            return Result.Failure(LockerErrors.NotAvailable);

        Status = LockerStatus.Assigned;
        AssignedMemberId = memberId;
        AssignedOnUtc = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    public Result Release()
    {
        if (Status != LockerStatus.Assigned)
            return Result.Failure(LockerErrors.NotAssigned);

        Status = LockerStatus.Available;
        AssignedMemberId = null;
        AssignedOnUtc = null;

        return Result.Success();
    }

    public Result SetUnderMaintenance()
    {
        if (Status == LockerStatus.Assigned)
            return Result.Failure(LockerErrors.NotAvailable);

        Status = LockerStatus.Maintenance;
        return Result.Success();
    }

    public void MakeAvailable() => Status = LockerStatus.Available;

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
