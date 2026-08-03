using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Trainers;

/// <summary>A recurring weekly window during which a trainer is available to be scheduled for classes.</summary>
public sealed class AvailabilitySlot : ValueObject
{
    private AvailabilitySlot()
    {
    }

    internal AvailabilitySlot(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime)
    {
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
    }

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DayOfWeek;
        yield return StartTime;
        yield return EndTime;
    }
}
