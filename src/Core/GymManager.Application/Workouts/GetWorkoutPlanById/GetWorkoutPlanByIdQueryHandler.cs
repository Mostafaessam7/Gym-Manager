using GymManager.Application.Abstractions;
using GymManager.Application.Workouts.Contracts;
using GymManager.Domain.Workouts.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Workouts.GetWorkoutPlanById;

public sealed class GetWorkoutPlanByIdQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetWorkoutPlanByIdQuery, Result<WorkoutPlanResponse>>
{
    public async Task<Result<WorkoutPlanResponse>> Handle(GetWorkoutPlanByIdQuery query, CancellationToken cancellationToken)
    {
        var plan = await readDb.WorkoutPlans.FirstOrDefaultAsync(p => p.Id == query.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<WorkoutPlanResponse>(WorkoutErrors.PlanNotFound);

        // IgnoreQueryFilters(): this lookup only resolves the owning member's branch to authorize the caller
        // against it — see GetMembershipsByMemberQueryHandler for why the global branch-isolation filter
        // would otherwise turn this into a silent cross-branch data leak.
        var member = await readDb.Members.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == plan.MemberId, cancellationToken);
        if (member is not null)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
            if (accessResult.IsFailure)
                return Result.Failure<WorkoutPlanResponse>(accessResult.Error);
        }

        return Result.Success(plan.ToResponse());
    }
}
