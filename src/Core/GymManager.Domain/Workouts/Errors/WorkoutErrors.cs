using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Workouts.Errors;

public static class WorkoutErrors
{
    public static readonly Error PlanNotFound = Error.NotFound("Workout.PlanNotFound", "The workout plan was not found.");

    public static readonly Error ExerciseNotFound = Error.NotFound("Workout.ExerciseNotFound", "The exercise was not found on this plan.");

    public static readonly Error LogNotFound = Error.NotFound("Workout.LogNotFound", "The workout log was not found.");
}
