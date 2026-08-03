using GymManager.Domain.Workouts.Errors;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Workouts;

/// <summary>A prescribed training program for a member — a named collection of exercises, optionally
/// organized across several days of a repeating split, assigned by a trainer (or self-assigned, if
/// <see cref="TrainerId"/> is null).</summary>
public sealed class WorkoutPlan : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<WorkoutPlanExercise> _exercises = [];

    private WorkoutPlan()
    {
        Name = string.Empty;
    }

    private WorkoutPlan(Guid id, Guid memberId, Guid? trainerId, string name, string? description)
        : base(id)
    {
        MemberId = memberId;
        TrainerId = trainerId;
        Name = name;
        Description = description;
        IsActive = true;
    }

    public Guid MemberId { get; private set; }

    public Guid? TrainerId { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<WorkoutPlanExercise> Exercises => _exercises.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static WorkoutPlan Create(Guid memberId, Guid? trainerId, string name, string? description) =>
        new(Guid.NewGuid(), memberId, trainerId, name.Trim(), description?.Trim());

    public void UpdateDetails(string name, string? description)
    {
        Name = name.Trim();
        Description = description?.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public WorkoutPlanExercise AddExercise(
        string exerciseName, int dayNumber, int order, int? sets, int? reps,
        decimal? weightKg, int? durationSeconds, int? restSeconds, string? notes)
    {
        var exercise = new WorkoutPlanExercise(
            exerciseName.Trim(), dayNumber, order, sets, reps, weightKg, durationSeconds, restSeconds, notes?.Trim());
        _exercises.Add(exercise);
        return exercise;
    }

    public Result UpdateExercise(
        Guid exerciseId, string exerciseName, int dayNumber, int order, int? sets, int? reps,
        decimal? weightKg, int? durationSeconds, int? restSeconds, string? notes)
    {
        var exercise = _exercises.FirstOrDefault(e => e.Id == exerciseId);
        if (exercise is null)
            return Result.Failure(WorkoutErrors.ExerciseNotFound);

        exercise.Update(exerciseName.Trim(), dayNumber, order, sets, reps, weightKg, durationSeconds, restSeconds, notes?.Trim());
        return Result.Success();
    }

    public Result RemoveExercise(Guid exerciseId)
    {
        var exercise = _exercises.FirstOrDefault(e => e.Id == exerciseId);
        if (exercise is null)
            return Result.Failure(WorkoutErrors.ExerciseNotFound);

        _exercises.Remove(exercise);
        return Result.Success();
    }

    public void SetCreated(DateTimeOffset onUtc, string? by)
    {
        CreatedOnUtc = onUtc;
        CreatedBy = by;
    }

    public void SetModified(DateTimeOffset onUtc, string? by)
    {
        ModifiedOnUtc = onUtc;
        ModifiedBy = by;
    }
}
