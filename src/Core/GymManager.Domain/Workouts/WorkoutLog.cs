using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Workouts;

/// <summary>A record of one completed (or partially completed) training session, optionally tied back to a
/// <see cref="WorkoutPlan"/> the member was following.</summary>
public sealed class WorkoutLog : AggregateRoot<Guid>
{
    private readonly List<WorkoutLogExercise> _exercises = [];

    private WorkoutLog()
    {
    }

    private WorkoutLog(Guid id, Guid memberId, Guid? workoutPlanId, DateTimeOffset completedOnUtc, int? durationMinutes, string? notes)
        : base(id)
    {
        MemberId = memberId;
        WorkoutPlanId = workoutPlanId;
        CompletedOnUtc = completedOnUtc;
        DurationMinutes = durationMinutes;
        Notes = notes;
    }

    public Guid MemberId { get; private set; }

    public Guid? WorkoutPlanId { get; private set; }

    public DateTimeOffset CompletedOnUtc { get; private set; }

    public int? DurationMinutes { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyCollection<WorkoutLogExercise> Exercises => _exercises.AsReadOnly();

    public static WorkoutLog Record(Guid memberId, Guid? workoutPlanId, DateTimeOffset completedOnUtc, int? durationMinutes, string? notes) =>
        new(Guid.NewGuid(), memberId, workoutPlanId, completedOnUtc, durationMinutes, notes?.Trim());

    public WorkoutLogExercise AddExercise(string exerciseName, int? setsCompleted, int? repsCompleted, decimal? weightKg, string? notes)
    {
        var exercise = new WorkoutLogExercise(exerciseName.Trim(), setsCompleted, repsCompleted, weightKg, notes?.Trim());
        _exercises.Add(exercise);
        return exercise;
    }
}
