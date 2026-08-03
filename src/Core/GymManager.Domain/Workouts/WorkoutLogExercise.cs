using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Workouts;

/// <summary>What was actually performed for one exercise during a logged session — independent of the plan's
/// prescribed sets/reps, since a member may do more, fewer, or substitute entirely.</summary>
public sealed class WorkoutLogExercise : Entity<Guid>
{
    private WorkoutLogExercise()
    {
        ExerciseName = string.Empty;
    }

    internal WorkoutLogExercise(string exerciseName, int? setsCompleted, int? repsCompleted, decimal? weightKg, string? notes)
        : base(Guid.NewGuid())
    {
        ExerciseName = exerciseName;
        SetsCompleted = setsCompleted;
        RepsCompleted = repsCompleted;
        WeightKg = weightKg;
        Notes = notes;
    }

    public string ExerciseName { get; private set; }

    public int? SetsCompleted { get; private set; }

    public int? RepsCompleted { get; private set; }

    public decimal? WeightKg { get; private set; }

    public string? Notes { get; private set; }
}
