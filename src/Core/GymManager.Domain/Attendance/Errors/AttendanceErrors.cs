using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Attendance.Errors;

public static class AttendanceErrors
{
    public static readonly Error NotFound = Error.NotFound("Attendance.NotFound", "The attendance record was not found.");

    public static readonly Error AlreadyCheckedIn = Error.Conflict("Attendance.AlreadyCheckedIn", "This member already has an open check-in.");

    public static readonly Error AlreadyCheckedOut = Error.Conflict("Attendance.AlreadyCheckedOut", "This attendance record is already checked out.");

    public static readonly Error NoOpenSession = Error.NotFound("Attendance.NoOpenSession", "This member has no open check-in to check out of.");

    public static readonly Error InvalidCheckInCode = Error.NotFound("Attendance.InvalidCheckInCode", "No member matches this check-in code.");

    public static readonly Error MembershipNotActive = Error.Forbidden("Attendance.MembershipNotActive", "This member does not have an active membership.");

    public static readonly Error MemberNotActive = Error.Forbidden("Attendance.MemberNotActive", "This member's account is frozen or inactive.");
}
