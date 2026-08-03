using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Attendance.Events;

public sealed record MemberCheckedInDomainEvent(Guid AttendanceRecordId, Guid MemberId, Guid BranchId) : IDomainEvent;

public sealed record MemberCheckedOutDomainEvent(Guid AttendanceRecordId, Guid MemberId) : IDomainEvent;
