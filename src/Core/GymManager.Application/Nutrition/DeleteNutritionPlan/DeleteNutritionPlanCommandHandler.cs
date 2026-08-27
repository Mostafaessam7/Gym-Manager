using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Nutrition;
using GymManager.Domain.Nutrition.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Nutrition.DeleteNutritionPlan;

public sealed class DeleteNutritionPlanCommandHandler(
    INutritionPlanRepository nutritionPlanRepository, IUnitOfWork unitOfWork, IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<DeleteNutritionPlanCommand>
{
    public async Task<Result> Handle(DeleteNutritionPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await nutritionPlanRepository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure(NutritionErrors.PlanNotFound);

        // IgnoreQueryFilters(): see GetMembershipsByMemberQueryHandler for why this authorization-only lookup
        // must bypass the global branch-isolation filter.
        var member = await readDb.Members.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == plan.MemberId, cancellationToken);
        if (member is not null)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
            if (accessResult.IsFailure)
                return Result.Failure(accessResult.Error);
        }

        nutritionPlanRepository.Remove(plan);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
