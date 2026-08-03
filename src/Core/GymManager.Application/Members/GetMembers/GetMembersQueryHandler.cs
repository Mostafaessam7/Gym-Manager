using GymManager.Application.Abstractions;
using GymManager.Application.Members.Contracts;
using GymManager.Domain.Members;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Members.GetMembers;

public sealed class GetMembersQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetMembersQuery, PagedList<MemberResponse>>
{
    public async Task<PagedList<MemberResponse>> Handle(GetMembersQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var members = readDb.Members.AsQueryable();

        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        if (branchId.HasValue)
            members = members.Where(m => m.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<MemberStatus>(query.Status, true, out var status))
            members = members.Where(m => m.Status == status);

        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            var term = pagination.SearchTerm.Trim().ToLower();
            members = members.Where(m =>
                m.FirstName.ToLower().Contains(term) ||
                m.LastName.ToLower().Contains(term) ||
                m.MemberCode.ToLower().Contains(term) ||
                m.PhoneNumber.Contains(term));
        }

        members = pagination.SortBy?.ToLowerInvariant() switch
        {
            "firstname" => pagination.SortDescending ? members.OrderByDescending(m => m.FirstName) : members.OrderBy(m => m.FirstName),
            "joinedon" => pagination.SortDescending ? members.OrderByDescending(m => m.JoinedOnUtc) : members.OrderBy(m => m.JoinedOnUtc),
            _ => members.OrderByDescending(m => m.CreatedOnUtc),
        };

        var totalCount = await members.CountAsync(cancellationToken);

        var items = await members
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<MemberResponse>(items.Select(m => m.ToResponse()).ToList(), pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
