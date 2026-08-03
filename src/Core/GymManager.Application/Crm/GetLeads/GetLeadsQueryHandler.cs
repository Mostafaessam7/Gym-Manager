using GymManager.Application.Abstractions;
using GymManager.Application.Crm.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Crm.GetLeads;

public sealed class GetLeadsQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard) : IQueryHandler<GetLeadsQuery, PagedList<LeadResponse>>
{
    public async Task<PagedList<LeadResponse>> Handle(GetLeadsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var leads = readDb.Leads.AsQueryable();

        if (query.Stage.HasValue)
            leads = leads.Where(l => l.Stage == query.Stage);

        if (query.AssignedToUserId.HasValue)
            leads = leads.Where(l => l.AssignedToUserId == query.AssignedToUserId);

        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        if (branchId.HasValue)
            leads = leads.Where(l => l.BranchId == branchId);

        var totalCount = await leads.CountAsync(cancellationToken);

        var page = await leads
            .OrderByDescending(l => l.CreatedOnUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var items = page.Select(l => l.ToResponse()).ToList();

        return new PagedList<LeadResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
