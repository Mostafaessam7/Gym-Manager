using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Workouts.DeleteWorkoutPlan;

public sealed record DeleteWorkoutPlanCommand(Guid PlanId) : ICommand;
