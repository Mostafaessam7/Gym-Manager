using GymManager.Domain.Workouts;

namespace GymManager.Application.Workouts.Contracts;

public static class WorkoutMappingExtensions
{
    public static WorkoutPlanResponse ToResponse(this WorkoutPlan plan) => new(
        plan.Id,
        plan.MemberId,
        plan.TrainerId,
        plan.Name,
        plan.Description,
        plan.IsActive,
        [.. plan.Exercises
            .OrderBy(e => e.DayNumber).ThenBy(e => e.Order)
            .Select(e => new WorkoutPlanExerciseResponse(
                e.Id, e.ExerciseName, e.DayNumber, e.Order, e.Sets, e.Reps, e.WeightKg, e.DurationSeconds, e.RestSeconds, e.Notes))],
        plan.CreatedOnUtc);

    public static WorkoutLogResponse ToResponse(this WorkoutLog log) => new(
        log.Id,
        log.MemberId,
        log.WorkoutPlanId,
        log.CompletedOnUtc,
        log.DurationMinutes,
        log.Notes,
        [.. log.Exercises.Select(e => new WorkoutLogExerciseResponse(e.Id, e.ExerciseName, e.SetsCompleted, e.RepsCompleted, e.WeightKg, e.Notes))]);
}
