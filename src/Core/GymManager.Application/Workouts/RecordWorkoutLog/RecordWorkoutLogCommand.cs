using GymManager.Application.Workouts.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Workouts.RecordWorkoutLog;

public sealed record WorkoutLogExerciseInput(string ExerciseName, int? SetsCompleted, int? RepsCompleted, decimal? WeightKg, string? Notes);

public sealed record RecordWorkoutLogCommand(
    Guid MemberId,
    Guid? WorkoutPlanId,
    DateTimeOffset CompletedOnUtc,
    int? DurationMinutes,
    string? Notes,
    IReadOnlyCollection<WorkoutLogExerciseInput> Exercises) : ICommand<Result<WorkoutLogResponse>>;
