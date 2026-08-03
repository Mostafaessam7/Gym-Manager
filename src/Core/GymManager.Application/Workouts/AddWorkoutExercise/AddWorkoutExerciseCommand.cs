using GymManager.Application.Workouts.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Workouts.AddWorkoutExercise;

public sealed record AddWorkoutExerciseCommand(Guid PlanId, WorkoutExerciseInput Exercise) : ICommand<Result<WorkoutPlanExerciseResponse>>;
