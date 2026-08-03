using GymManager.Application.Abstractions;
using GymManager.Application.Memberships.Plans.GetPlans;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Memberships;
using GymManager.Domain.Memberships.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Memberships.Plans.DeactivatePlan;

public sealed class DeactivatePlanCommandHandler(
    IMembershipPlanRepository planRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard, ICacheService cacheService)
    : ICommandHandler<DeactivatePlanCommand>
{
    public async Task<Result> Handle(DeactivatePlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await planRepository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure(MembershipPlanErrors.NotFound);

        if (plan.BranchId.HasValue)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(plan.BranchId.Value);
            if (accessResult.IsFailure)
                return accessResult;
        }

        plan.Deactivate();

        planRepository.Update(plan);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        GetPlansQueryHandler.InvalidateCache(cacheService);

        return Result.Success();
    }
}
