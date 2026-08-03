namespace GymManager.Application.Workouts.Contracts;

public sealed record WorkoutExerciseInput(
    string ExerciseName,
    int DayNumber,
    int Order,
    int? Sets,
    int? Reps,
    decimal? WeightKg,
    int? DurationSeconds,
    int? RestSeconds,
    string? Notes);

public sealed record WorkoutPlanExerciseResponse(
    Guid Id,
    string ExerciseName,
    int DayNumber,
    int Order,
    int? Sets,
    int? Reps,
    decimal? WeightKg,
    int? DurationSeconds,
    int? RestSeconds,
    string? Notes);

public sealed record WorkoutPlanResponse(
    Guid Id,
    Guid MemberId,
    Guid? TrainerId,
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyCollection<WorkoutPlanExerciseResponse> Exercises,
    DateTimeOffset CreatedOnUtc);

public sealed record WorkoutLogExerciseResponse(
    Guid Id, string ExerciseName, int? SetsCompleted, int? RepsCompleted, decimal? WeightKg, string? Notes);

public sealed record WorkoutLogResponse(
    Guid Id,
    Guid MemberId,
    Guid? WorkoutPlanId,
    DateTimeOffset CompletedOnUtc,
    int? DurationMinutes,
    string? Notes,
    IReadOnlyCollection<WorkoutLogExerciseResponse> Exercises);
