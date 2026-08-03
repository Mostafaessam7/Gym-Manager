using GymManager.Application.Workouts.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Workouts.GetWorkoutPlanById;

public sealed record GetWorkoutPlanByIdQuery(Guid PlanId) : IQuery<Result<WorkoutPlanResponse>>;
