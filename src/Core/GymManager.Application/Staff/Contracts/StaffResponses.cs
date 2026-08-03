namespace GymManager.Application.Staff.Contracts;

public sealed record StaffShiftResponse(
    Guid Id, Guid UserId, Guid BranchId, DateTimeOffset StartUtc, DateTimeOffset EndUtc, string Status, string? Notes);

public sealed record LeaveRequestResponse(
    Guid Id,
    Guid UserId,
    string Type,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason,
    string Status,
    Guid? DecidedByUserId,
    DateTimeOffset? DecidedOnUtc,
    string? DecisionNotes);

public sealed record CommissionResponse(
    Guid Id,
    Guid UserId,
    decimal Amount,
    string Currency,
    string SourceType,
    Guid? SourceReferenceId,
    DateTimeOffset EarnedOnUtc,
    string? Notes,
    string Status,
    DateTimeOffset? PaidOnUtc);
