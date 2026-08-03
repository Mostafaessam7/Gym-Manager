using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Nutrition;
using GymManager.Domain.Nutrition.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Nutrition.UpdateNutritionPlan;

public sealed class UpdateNutritionPlanCommandHandler(
    INutritionPlanRepository nutritionPlanRepository, IUnitOfWork unitOfWork, IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpdateNutritionPlanCommand>
{
    public async Task<Result> Handle(UpdateNutritionPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await nutritionPlanRepository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure(NutritionErrors.PlanNotFound);

        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == plan.MemberId, cancellationToken);
        if (member is not null)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
            if (accessResult.IsFailure)
                return Result.Failure(accessResult.Error);
        }

        plan.UpdateDetails(
            command.Name, command.Description, command.DailyCalorieTarget, command.ProteinTargetG, command.CarbsTargetG, command.FatTargetG);

        if (command.IsActive)
            plan.Activate();
        else
            plan.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
