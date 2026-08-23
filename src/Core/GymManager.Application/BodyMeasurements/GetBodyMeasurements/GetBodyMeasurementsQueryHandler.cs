using GymManager.Application.Abstractions;
using GymManager.Application.BodyMeasurements.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.BodyMeasurements.GetBodyMeasurements;

public sealed class GetBodyMeasurementsQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetBodyMeasurementsQuery, PagedList<BodyMeasurementResponse>>
{
    public async Task<PagedList<BodyMeasurementResponse>> Handle(GetBodyMeasurementsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var measurements = readDb.BodyMeasurements.Where(b => b.MemberId == query.MemberId);

        // BodyMeasurement has no BranchId of its own — it's scoped through the member's own branch, same
        // pattern as NutritionPlan/WorkoutPlan. A branch-scoped caller who names a member outside their
        // branch simply sees an empty page rather than a 403, matching how every other member-scoped list
        // query in this codebase (Nutrition, Workouts) already behaves for consistency.
        var branchId = branchAccessGuard.ResolveFilter(null);
        if (branchId.HasValue)
        {
            var branchMemberIds = readDb.Members.Where(m => m.BranchId == branchId).Select(m => m.Id);
            measurements = measurements.Where(b => branchMemberIds.Contains(b.MemberId));
        }

        var totalCount = await measurements.CountAsync(cancellationToken);

        var page = await measurements
            .OrderByDescending(b => b.RecordedOnUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        // Mapped after materialization (not inside the LINQ-to-SQL query) because BodyMeasurement.Bmi is a
        // computed property with branching logic that EF Core cannot translate to SQL.
        var items = page.Select(b => b.ToResponse()).ToList();

        return new PagedList<BodyMeasurementResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
