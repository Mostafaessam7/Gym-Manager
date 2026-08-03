using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Memberships.Plans.UpdatePlan;

public sealed record UpdatePlanCommand(
    Guid PlanId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int DurationInDays,
    int MaxFreezeDays) : ICommand;
