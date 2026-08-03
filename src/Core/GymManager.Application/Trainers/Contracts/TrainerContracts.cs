namespace GymManager.Application.Trainers.Contracts;

public sealed record AvailabilitySlotResponse(string DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

public sealed record TrainerResponse(
    Guid Id,
    Guid BranchId,
    Guid? UserId,
    string FirstName,
    string LastName,
    string Specialization,
    string? Bio,
    string? PhoneNumber,
    string? Email,
    bool IsActive,
    DateTimeOffset HireDateUtc,
    IReadOnlyCollection<AvailabilitySlotResponse> Availability);
