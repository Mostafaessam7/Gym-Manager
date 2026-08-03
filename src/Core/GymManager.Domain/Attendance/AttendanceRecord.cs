using GymManager.Domain.Attendance.Errors;
using GymManager.Domain.Attendance.Events;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Attendance;

/// <summary>A single gym visit: a check-in, optionally paired with a later check-out.</summary>
public sealed class AttendanceRecord : AggregateRoot<Guid>
{
    private AttendanceRecord()
    {
    }

    private AttendanceRecord(Guid id, Guid memberId, Guid branchId, CheckInMethod method, Guid? checkedInByUserId) : base(id)
    {
        MemberId = memberId;
        BranchId = branchId;
        Method = method;
        CheckedInByUserId = checkedInByUserId;
        CheckInUtc = DateTimeOffset.UtcNow;
    }

    public Guid MemberId { get; private set; }

    public Guid BranchId { get; private set; }

    public CheckInMethod Method { get; private set; }

    /// <summary>The staff user who performed a manual check-in, or null for self-service QR/barcode scans.</summary>
    public Guid? CheckedInByUserId { get; private set; }

    public DateTimeOffset CheckInUtc { get; private set; }

    public DateTimeOffset? CheckOutUtc { get; private set; }

    public bool IsOpen => CheckOutUtc is null;

    public static AttendanceRecord CheckIn(Guid memberId, Guid branchId, CheckInMethod method, Guid? checkedInByUserId = null)
    {
        var record = new AttendanceRecord(Guid.NewGuid(), memberId, branchId, method, checkedInByUserId);
        record.Raise(new MemberCheckedInDomainEvent(record.Id, memberId, branchId));
        return record;
    }

    public Result CheckOut()
    {
        if (!IsOpen)
            return Result.Failure(AttendanceErrors.AlreadyCheckedOut);

        CheckOutUtc = DateTimeOffset.UtcNow;
        Raise(new MemberCheckedOutDomainEvent(Id, MemberId));
        return Result.Success();
    }
}
