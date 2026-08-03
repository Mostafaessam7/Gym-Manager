using GymManager.Application.Abstractions;
using GymManager.Application.Workouts.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Workouts.GetWorkoutLogs;

public sealed class GetWorkoutLogsQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetWorkoutLogsQuery, PagedList<WorkoutLogResponse>>
{
    public async Task<PagedList<WorkoutLogResponse>> Handle(GetWorkoutLogsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var logs = readDb.WorkoutLogs.Where(l => l.MemberId == query.MemberId);

        // WorkoutLog has no BranchId of its own — it's scoped through the member's own branch.
        var branchId = branchAccessGuard.ResolveFilter(null);
        if (branchId.HasValue)
        {
            var branchMemberIds = readDb.Members.Where(m => m.BranchId == branchId).Select(m => m.Id);
            logs = logs.Where(l => branchMemberIds.Contains(l.MemberId));
        }

        var totalCount = await logs.CountAsync(cancellationToken);

        var page = await logs
            .OrderByDescending(l => l.CompletedOnUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var items = page.Select(l => l.ToResponse()).ToList();

        return new PagedList<WorkoutLogResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
