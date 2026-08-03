using GymManager.Application.Abstractions;
using GymManager.Application.Workouts.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Workouts.GetWorkoutPlans;

public sealed class GetWorkoutPlansQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetWorkoutPlansQuery, PagedList<WorkoutPlanResponse>>
{
    public async Task<PagedList<WorkoutPlanResponse>> Handle(GetWorkoutPlansQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var plans = readDb.WorkoutPlans.Where(p => p.MemberId == query.MemberId);

        // WorkoutPlan has no BranchId of its own — it's scoped through the member's own branch.
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

        return new PagedList<WorkoutPlanResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
