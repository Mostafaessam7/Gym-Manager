using GymManager.Domain.Classes.Errors;
using GymManager.Domain.Classes.Events;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Classes;

/// <summary>A single bookable occurrence of a <see cref="GymClass"/> at a specific time.</summary>
public sealed class ClassSession : AggregateRoot<Guid>
{
    private readonly List<ClassBooking> _bookings = [];

    private ClassSession()
    {
    }

    private ClassSession(Guid id, Guid gymClassId, Guid trainerId, Guid branchId, DateTimeOffset startUtc, DateTimeOffset endUtc, int capacity)
        : base(id)
    {
        GymClassId = gymClassId;
        TrainerId = trainerId;
        BranchId = branchId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Capacity = capacity;
        Status = ClassSessionStatus.Scheduled;
    }

    public Guid GymClassId { get; private set; }

    public Guid TrainerId { get; private set; }

    public Guid BranchId { get; private set; }

    public DateTimeOffset StartUtc { get; private set; }

    public DateTimeOffset EndUtc { get; private set; }

    public int Capacity { get; private set; }

    public ClassSessionStatus Status { get; private set; }

    public IReadOnlyCollection<ClassBooking> Bookings => _bookings.AsReadOnly();

    public int ActiveBookingsCount => _bookings.Count(b => b.Status != BookingStatus.Cancelled);

    public bool HasAvailableSpots => ActiveBookingsCount < Capacity;

    public static Result<ClassSession> Schedule(Guid gymClassId, Guid trainerId, Guid branchId, DateTimeOffset startUtc, DateTimeOffset endUtc, int capacity)
    {
        if (endUtc <= startUtc)
            return Result.Failure<ClassSession>(ClassSessionErrors.EndBeforeStart);

        var session = new ClassSession(Guid.NewGuid(), gymClassId, trainerId, branchId, startUtc, endUtc, capacity);
        session.Raise(new ClassSessionScheduledDomainEvent(session.Id, gymClassId, trainerId));

        return Result.Success(session);
    }

    public Result Book(Guid memberId)
    {
        if (Status != ClassSessionStatus.Scheduled)
            return Result.Failure(ClassSessionErrors.NotScheduled);

        if (_bookings.Any(b => b.MemberId == memberId && b.Status == BookingStatus.Booked))
            return Result.Failure(ClassSessionErrors.AlreadyBooked);

        if (!HasAvailableSpots)
            return Result.Failure(ClassSessionErrors.SessionFull);

        _bookings.Add(new ClassBooking(memberId));
        Raise(new ClassSessionBookedDomainEvent(Id, memberId));

        return Result.Success();
    }

    public Result CancelBooking(Guid memberId)
    {
        var booking = _bookings.FirstOrDefault(b => b.MemberId == memberId && b.Status == BookingStatus.Booked);
        if (booking is null)
            return Result.Failure(ClassSessionErrors.BookingNotFound);

        booking.Cancel();
        Raise(new ClassBookingCancelledDomainEvent(Id, memberId));

        return Result.Success();
    }

    public Result MarkAttended(Guid memberId)
    {
        var booking = _bookings.FirstOrDefault(b => b.MemberId == memberId && b.Status == BookingStatus.Booked);
        if (booking is null)
            return Result.Failure(ClassSessionErrors.BookingNotFound);

        booking.MarkAttended();
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == ClassSessionStatus.Cancelled)
            return Result.Failure(ClassSessionErrors.AlreadyCancelled);

        Status = ClassSessionStatus.Cancelled;
        foreach (var booking in _bookings.Where(b => b.Status == BookingStatus.Booked))
            booking.Cancel();

        Raise(new ClassSessionCancelledDomainEvent(Id));
        return Result.Success();
    }

    public void Complete()
    {
        if (Status == ClassSessionStatus.Scheduled)
            Status = ClassSessionStatus.Completed;
    }
}
