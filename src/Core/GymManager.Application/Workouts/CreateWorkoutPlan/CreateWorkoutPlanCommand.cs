using GymManager.Application.Workouts.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Workouts.CreateWorkoutPlan;

public sealed record CreateWorkoutPlanCommand(
    Guid MemberId, Guid? TrainerId, string Name, string? Description, IReadOnlyCollection<WorkoutExerciseInput> Exercises)
    : ICommand<Result<WorkoutPlanResponse>>;
