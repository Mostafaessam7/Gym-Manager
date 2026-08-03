using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Workouts.UpdateWorkoutPlan;

public sealed record UpdateWorkoutPlanCommand(Guid PlanId, string Name, string? Description, bool IsActive) : ICommand;
