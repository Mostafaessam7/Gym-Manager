using GymManager.Application.Abstractions;
using GymManager.Application.Nutrition.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members.Errors;
using GymManager.Domain.Nutrition;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Nutrition.CreateNutritionPlan;

public sealed class CreateNutritionPlanCommandHandler(
    IApplicationReadDb readDb, INutritionPlanRepository nutritionPlanRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CreateNutritionPlanCommand, Result<NutritionPlanResponse>>
{
    public async Task<Result<NutritionPlanResponse>> Handle(CreateNutritionPlanCommand command, CancellationToken cancellationToken)
    {
        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == command.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure<NutritionPlanResponse>(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<NutritionPlanResponse>(accessResult.Error);

        var plan = NutritionPlan.Create(
            command.MemberId, command.TrainerId, command.Name, command.Description,
            command.DailyCalorieTarget, command.ProteinTargetG, command.CarbsTargetG, command.FatTargetG);

        foreach (var meal in command.Meals)
            plan.AddMeal(meal.Name, meal.Order, meal.TimeOfDay, meal.Calories, meal.ProteinG, meal.CarbsG, meal.FatG, meal.Notes);

        nutritionPlanRepository.Add(plan);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(plan.ToResponse());
    }
}
