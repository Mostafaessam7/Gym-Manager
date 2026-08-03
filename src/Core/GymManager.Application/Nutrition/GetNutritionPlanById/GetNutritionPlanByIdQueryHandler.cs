using GymManager.Application.Abstractions;
using GymManager.Application.Nutrition.Contracts;
using GymManager.Domain.Nutrition.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Nutrition.GetNutritionPlanById;

public sealed class GetNutritionPlanByIdQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetNutritionPlanByIdQuery, Result<NutritionPlanResponse>>
{
    public async Task<Result<NutritionPlanResponse>> Handle(GetNutritionPlanByIdQuery query, CancellationToken cancellationToken)
    {
        var plan = await readDb.NutritionPlans.FirstOrDefaultAsync(p => p.Id == query.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<NutritionPlanResponse>(NutritionErrors.PlanNotFound);

        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == plan.MemberId, cancellationToken);
        if (member is not null)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
            if (accessResult.IsFailure)
                return Result.Failure<NutritionPlanResponse>(accessResult.Error);
        }

        return Result.Success(plan.ToResponse());
    }
}
