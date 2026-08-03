using GymManager.Domain.Classes;

namespace GymManager.Application.Classes.Contracts;

public static class ClassMappingExtensions
{
    public static GymClassResponse ToResponse(this GymClass gymClass) => new(
        gymClass.Id, gymClass.Name, gymClass.Description, gymClass.BranchId, gymClass.TrainerId,
        gymClass.Capacity, gymClass.DurationMinutes, gymClass.IsActive);

    public static ClassSessionResponse ToResponse(this ClassSession session) => new(
        session.Id, session.GymClassId, session.TrainerId, session.BranchId, session.StartUtc, session.EndUtc,
        session.Capacity, session.ActiveBookingsCount, session.Status.ToString(),
        session.Bookings.Select(b => new ClassBookingResponse(b.MemberId, b.Status.ToString(), b.BookedOnUtc)).ToArray());
}
