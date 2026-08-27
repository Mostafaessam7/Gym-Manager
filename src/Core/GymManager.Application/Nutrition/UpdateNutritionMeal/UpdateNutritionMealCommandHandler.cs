using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Nutrition;
using GymManager.Domain.Nutrition.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Nutrition.UpdateNutritionMeal;

public sealed class UpdateNutritionMealCommandHandler(
    INutritionPlanRepository nutritionPlanRepository, IUnitOfWork unitOfWork, IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpdateNutritionMealCommand>
{
    public async Task<Result> Handle(UpdateNutritionMealCommand command, CancellationToken cancellationToken)
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

        var result = plan.UpdateMeal(
            command.MealId, command.Meal.Name, command.Meal.Order, command.Meal.TimeOfDay, command.Meal.Calories,
            command.Meal.ProteinG, command.Meal.CarbsG, command.Meal.FatG, command.Meal.Notes);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
