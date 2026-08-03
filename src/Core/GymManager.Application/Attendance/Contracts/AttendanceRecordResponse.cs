namespace GymManager.Application.Attendance.Contracts;

public sealed record AttendanceRecordResponse(
    Guid Id,
    Guid MemberId,
    string MemberFullName,
    Guid BranchId,
    string Method,
    DateTimeOffset CheckInUtc,
    DateTimeOffset? CheckOutUtc);
