using GymManager.Application.Abstractions;
using GymManager.Application.Classes.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Classes.GetGymClasses;

public sealed class GetGymClassesQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetGymClassesQuery, PagedList<GymClassResponse>>
{
    public async Task<PagedList<GymClassResponse>> Handle(GetGymClassesQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var classes = readDb.GymClasses.AsQueryable();

        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        if (branchId.HasValue)
            classes = classes.Where(c => c.BranchId == branchId);

        if (!query.IncludeInactive)
            classes = classes.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            var term = pagination.SearchTerm.Trim().ToLower();
            classes = classes.Where(c => c.Name.ToLower().Contains(term));
        }

        classes = pagination.SortDescending ? classes.OrderByDescending(c => c.Name) : classes.OrderBy(c => c.Name);

        var totalCount = await classes.CountAsync(cancellationToken);

        var items = await classes
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<GymClassResponse>(items.Select(c => c.ToResponse()).ToList(), pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
