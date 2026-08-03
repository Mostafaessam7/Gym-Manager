namespace GymManager.Application.Classes.Contracts;

public sealed record GymClassResponse(
    Guid Id, string Name, string Description, Guid BranchId, Guid TrainerId, int Capacity, int DurationMinutes, bool IsActive);

public sealed record ClassBookingResponse(Guid MemberId, string Status, DateTimeOffset BookedOnUtc);

public sealed record ClassSessionResponse(
    Guid Id,
    Guid GymClassId,
    Guid TrainerId,
    Guid BranchId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int Capacity,
    int ActiveBookingsCount,
    string Status,
    IReadOnlyCollection<ClassBookingResponse> Bookings);
