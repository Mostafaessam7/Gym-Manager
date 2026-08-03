using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Memberships.Plans.DeactivatePlan;

public sealed record DeactivatePlanCommand(Guid PlanId) : ICommand;
