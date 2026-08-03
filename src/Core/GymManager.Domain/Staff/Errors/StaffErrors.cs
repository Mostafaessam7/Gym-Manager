using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Staff.Errors;

public static class StaffErrors
{
    public static readonly Error ShiftNotFound = Error.NotFound("Staff.ShiftNotFound", "The shift was not found.");

    public static readonly Error ShiftAlreadyFinalized = Error.Conflict(
        "Staff.ShiftAlreadyFinalized", "This shift has already been completed, cancelled, or marked a no-show.");

    public static readonly Error ShiftEndBeforeStart = Error.Validation(
        "Staff.ShiftEndBeforeStart", "The shift's end time must be after its start time.");

    public static readonly Error LeaveRequestNotFound = Error.NotFound("Staff.LeaveRequestNotFound", "The leave request was not found.");

    public static readonly Error LeaveRequestAlreadyDecided = Error.Conflict(
        "Staff.LeaveRequestAlreadyDecided", "This leave request has already been approved or rejected.");

    public static readonly Error LeaveEndBeforeStart = Error.Validation(
        "Staff.LeaveEndBeforeStart", "The leave's end date must be on or after its start date.");

    public static readonly Error CommissionNotFound = Error.NotFound("Staff.CommissionNotFound", "The commission record was not found.");

    public static readonly Error CommissionAlreadyPaid = Error.Conflict("Staff.CommissionAlreadyPaid", "This commission has already been paid.");
}
