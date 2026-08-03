using GymManager.Application.Abstractions;
using GymManager.Application.Nutrition.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Nutrition.GetNutritionPlans;

public sealed class GetNutritionPlansQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetNutritionPlansQuery, PagedList<NutritionPlanResponse>>
{
    public async Task<PagedList<NutritionPlanResponse>> Handle(GetNutritionPlansQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var plans = readDb.NutritionPlans.Where(p => p.MemberId == query.MemberId);

        // NutritionPlan has no BranchId of its own — it's scoped through the member's own branch.
        var branchId = branchAccessGuard.ResolveFilter(null);
        if (branchId.HasValue)
        {
            var branchMemberIds = readDb.Members.Where(m => m.BranchId == branchId).Select(m => m.Id);
            plans = plans.Where(p => branchMemberIds.Contains(p.MemberId));
        }

        var totalCount = await plans.CountAsync(cancellationToken);

        var page = await plans
            .OrderByDescending(p => p.CreatedOnUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var items = page.Select(p => p.ToResponse()).ToList();

        return new PagedList<NutritionPlanResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
