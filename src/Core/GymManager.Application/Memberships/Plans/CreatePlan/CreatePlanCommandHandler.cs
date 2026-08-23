using GymManager.Application.Abstractions;
using GymManager.Application.Memberships.Contracts;
using GymManager.Application.Memberships.Plans.GetPlans;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Memberships;
using GymManager.Domain.Memberships.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Memberships.Plans.CreatePlan;

public sealed class CreatePlanCommandHandler(
    IMembershipPlanRepository planRepository, IUnitOfWork unitOfWork, ICacheService cacheService, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CreatePlanCommand, Result<MembershipPlanResponse>>
{
    public async Task<Result<MembershipPlanResponse>> Handle(CreatePlanCommand command, CancellationToken cancellationToken)
    {
        // Guid.Empty is deliberate for a null BranchId (a global, gym-wide plan): a branch-scoped caller
        // (whose claim never equals Guid.Empty) is denied, exactly like targeting any other branch that
        // isn't theirs; an unscoped (HQ-level) caller is unaffected by the value passed in, per
        // IBranchAccessGuard.EnsureCanAccess's own semantics. Without this, omitting BranchId used to skip
        // the check entirely and let a branch-scoped caller create a plan visible to every branch.
        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId ?? Guid.Empty);
        if (accessResult.IsFailure)
            return Result.Failure<MembershipPlanResponse>(accessResult.Error);

        if (await planRepository.NameExistsAsync(command.Name.Trim(), cancellationToken))
            return Result.Failure<MembershipPlanResponse>(MembershipPlanErrors.NameAlreadyInUse(command.Name));

        var priceResult = Money.Create(command.Price, command.Currency);
        if (priceResult.IsFailure)
            return Result.Failure<MembershipPlanResponse>(priceResult.Error);

        var plan = MembershipPlan.Create(
            command.Name, command.Description, priceResult.Value, command.DurationInDays, command.MaxFreezeDays, command.BranchId);

        planRepository.Add(plan);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        GetPlansQueryHandler.InvalidateCache(cacheService);

        return Result.Success(plan.ToResponse());
    }
}
