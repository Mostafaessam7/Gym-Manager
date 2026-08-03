using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Workouts;

/// <summary>One prescribed exercise within a <see cref="WorkoutPlan"/> — a target, not a record of what was
/// actually done (see <see cref="WorkoutLog"/> for that).</summary>
public sealed class WorkoutPlanExercise : Entity<Guid>
{
    private WorkoutPlanExercise()
    {
        ExerciseName = string.Empty;
    }

    internal WorkoutPlanExercise(
        string exerciseName, int dayNumber, int order, int? sets, int? reps,
        decimal? weightKg, int? durationSeconds, int? restSeconds, string? notes)
        : base(Guid.NewGuid())
    {
        ExerciseName = exerciseName;
        DayNumber = dayNumber;
        Order = order;
        Sets = sets;
        Reps = reps;
        WeightKg = weightKg;
        DurationSeconds = durationSeconds;
        RestSeconds = restSeconds;
        Notes = notes;
    }

    public string ExerciseName { get; private set; }

    /// <summary>Which day of the plan's cycle this exercise belongs to (1-based) — lets a plan span a
    /// multi-day split (e.g. day 1 = push, day 2 = pull) rather than assuming a single daily routine.</summary>
    public int DayNumber { get; private set; }

    /// <summary>Display/execution order within its day.</summary>
    public int Order { get; private set; }

    public int? Sets { get; private set; }

    public int? Reps { get; private set; }

    public decimal? WeightKg { get; private set; }

    public int? DurationSeconds { get; private set; }

    public int? RestSeconds { get; private set; }

    public string? Notes { get; private set; }

    internal void Update(
        string exerciseName, int dayNumber, int order, int? sets, int? reps,
        decimal? weightKg, int? durationSeconds, int? restSeconds, string? notes)
    {
        ExerciseName = exerciseName;
        DayNumber = dayNumber;
        Order = order;
        Sets = sets;
        Reps = reps;
        WeightKg = weightKg;
        DurationSeconds = durationSeconds;
        RestSeconds = restSeconds;
        Notes = notes;
    }
}
