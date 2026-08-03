using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Classes;

/// <summary>A single member's reservation for a <see cref="ClassSession"/>.</summary>
public sealed class ClassBooking : Entity<Guid>
{
    private ClassBooking()
    {
    }

    internal ClassBooking(Guid memberId) : base(Guid.NewGuid())
    {
        MemberId = memberId;
        Status = BookingStatus.Booked;
        BookedOnUtc = DateTimeOffset.UtcNow;
    }

    public Guid MemberId { get; private set; }

    public BookingStatus Status { get; private set; }

    public DateTimeOffset BookedOnUtc { get; private set; }

    internal void Cancel() => Status = BookingStatus.Cancelled;

    internal void MarkAttended() => Status = BookingStatus.Attended;
}
