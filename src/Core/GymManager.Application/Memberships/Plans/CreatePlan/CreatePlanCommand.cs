using GymManager.Application.Memberships.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Memberships.Plans.CreatePlan;

public sealed record CreatePlanCommand(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int DurationInDays,
    int MaxFreezeDays,
    Guid? BranchId) : ICommand<Result<MembershipPlanResponse>>;
