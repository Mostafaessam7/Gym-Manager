using GymManager.Application.Abstractions;
using GymManager.Application.Nutrition.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Nutrition;
using GymManager.Domain.Nutrition.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Nutrition.AddNutritionMeal;

public sealed class AddNutritionMealCommandHandler(
    INutritionPlanRepository nutritionPlanRepository, IUnitOfWork unitOfWork, IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<AddNutritionMealCommand, Result<NutritionPlanMealResponse>>
{
    public async Task<Result<NutritionPlanMealResponse>> Handle(AddNutritionMealCommand command, CancellationToken cancellationToken)
    {
        var plan = await nutritionPlanRepository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<NutritionPlanMealResponse>(NutritionErrors.PlanNotFound);

        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == plan.MemberId, cancellationToken);
        if (member is not null)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
            if (accessResult.IsFailure)
                return Result.Failure<NutritionPlanMealResponse>(accessResult.Error);
        }

        var meal = plan.AddMeal(
            command.Meal.Name, command.Meal.Order, command.Meal.TimeOfDay, command.Meal.Calories,
            command.Meal.ProteinG, command.Meal.CarbsG, command.Meal.FatG, command.Meal.Notes);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new NutritionPlanMealResponse(
            meal.Id, meal.Name, meal.Order, meal.TimeOfDay, meal.Calories, meal.ProteinG, meal.CarbsG, meal.FatG, meal.Notes));
    }
}
