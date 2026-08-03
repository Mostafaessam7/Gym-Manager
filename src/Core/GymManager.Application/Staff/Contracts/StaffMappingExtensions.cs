using GymManager.Domain.Staff;

namespace GymManager.Application.Staff.Contracts;

public static class StaffMappingExtensions
{
    public static StaffShiftResponse ToResponse(this StaffShift shift) => new(
        shift.Id, shift.UserId, shift.BranchId, shift.StartUtc, shift.EndUtc, shift.Status.ToString(), shift.Notes);

    public static LeaveRequestResponse ToResponse(this LeaveRequest leave) => new(
        leave.Id, leave.UserId, leave.Type.ToString(), leave.StartDate, leave.EndDate, leave.Reason,
        leave.Status.ToString(), leave.DecidedByUserId, leave.DecidedOnUtc, leave.DecisionNotes);

    public static CommissionResponse ToResponse(this Commission commission) => new(
        commission.Id, commission.UserId, commission.Amount.Amount, commission.Amount.Currency, commission.SourceType.ToString(),
        commission.SourceReferenceId, commission.EarnedOnUtc, commission.Notes, commission.Status.ToString(), commission.PaidOnUtc);
}
