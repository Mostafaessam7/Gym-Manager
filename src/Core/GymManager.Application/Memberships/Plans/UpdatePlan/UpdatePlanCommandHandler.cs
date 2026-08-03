using GymManager.Application.Abstractions;
using GymManager.Application.Memberships.Plans.GetPlans;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Memberships;
using GymManager.Domain.Memberships.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Memberships.Plans.UpdatePlan;

public sealed class UpdatePlanCommandHandler(
    IMembershipPlanRepository planRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard, ICacheService cacheService)
    : ICommandHandler<UpdatePlanCommand>
{
    public async Task<Result> Handle(UpdatePlanCommand command, CancellationToken cancellationToken)
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

        var priceResult = Money.Create(command.Price, command.Currency);
        if (priceResult.IsFailure)
            return priceResult;

        plan.Update(command.Name, command.Description, priceResult.Value, command.DurationInDays, command.MaxFreezeDays);

        planRepository.Update(plan);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        GetPlansQueryHandler.InvalidateCache(cacheService);

        return Result.Success();
    }
}
